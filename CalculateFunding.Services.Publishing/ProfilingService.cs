using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;
using ProfilePatternKey = CalculateFunding.Models.Publishing.ProfilePatternKey;

namespace CalculateFunding.Services.Publishing
{
    public class ProfilingService : IProfilingService, IHealthChecker
    {
        private readonly IProfilingApiClient _profilingApiClient;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _profilingApiClientPolicy;

        public ProfilingService(
            ILogger logger, 
            IProfilingApiClient profilingApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(profilingApiClient, nameof(profilingApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.ProfilingApiClient, nameof(publishingResiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));

            _logger = logger;
            _profilingApiClient = profilingApiClient;
            _profilingApiClientPolicy = publishingResiliencePolicies.ProfilingApiClient;
        }

        public async Task<IEnumerable<ProfilePatternKey>> ProfileFundingLines(IEnumerable<FundingLine> fundingLines, 
            string fundingStreamId, 
            string fundingPeriodId, 
            IEnumerable<ProfilePatternKey> profilePatternKeys = null, 
            string providerType = null, 
            string providerSubType = null)
        {
            Guard.ArgumentNotNull(fundingLines, nameof(fundingLines));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            List<ProfilePatternKey> profilePatternKeysToReturn = new List<ProfilePatternKey>();

            Dictionary<decimal?, ProviderProfilingResponseModel> response =
                 new Dictionary<decimal?, ProviderProfilingResponseModel>();

            Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>> profilingResponses =
                new Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>>();

            if (fundingLines.IsNullOrEmpty())
            {
                throw new ArgumentNullException($"Null or empty publsihed profiling fundingLines.");
            }

            //Change the key to FundingLineCode + ProfilePatternKey + ProviderType + ProviderSubType
            var fundingValues = fundingLines
                .Where(_ => _.Value != null)
                .Select(k => new { k.FundingLineCode, k.Value })
                .Distinct()
                .ToList();

            if (fundingValues.IsNullOrEmpty() || fundingValues.Count == 0)
            {
                string errorMessage = "No Funding Values of Type Payment in the Funding Totals for updating.";

                _logger.Error(errorMessage);

                return profilePatternKeysToReturn;
            }

            foreach (var value in fundingValues)
            {
                ProviderProfilingRequestModel requestModel = ConstructProfilingRequest(fundingPeriodId,
                                                                fundingStreamId,
                                                                value.FundingLineCode,
                                                                value.Value,
                                                                profilePatternKey: profilePatternKeys?.FirstOrDefault(x => x.FundingLineCode == value.FundingLineCode)?.Key,
                                                                providerType: providerType,
                                                                providerSubType: providerSubType);

                ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponse = await GetProviderProfilePeriods(requestModel);

                if (providerProfilingResponse?.Content == null)
                {
                    string errorMessage = $"Failed to Get Profile Periods for updating for Requested FundingPeriodId: '{fundingPeriodId}' and FundingStreamId: '{fundingStreamId}'";

                    _logger.Error(errorMessage);

                    throw new NonRetriableException(errorMessage);
                }

                if (profilePatternKeysToReturn.All(x => x.FundingLineCode != value.FundingLineCode))
                {
                    profilePatternKeysToReturn.Add(new ProfilePatternKey() { 
                        FundingLineCode = value.FundingLineCode, 
                        Key = providerProfilingResponse.Content.ProfilePatternKey
                    });
                }
                
                AddOrUpdateResponseDictionary(ref response, value.Value, providerProfilingResponse.Content);

                AddOrUpdateDictionaryProfilingResponses(profilingResponses, value.FundingLineCode, response);
            }

            SaveFundingLineTotals(ref fundingLines, profilingResponses);

            return profilePatternKeysToReturn;
        }

        public async Task<IEnumerable<FundingStreamPeriodProfilePattern>> GetProfilePatternsForFundingStreamAndFundingPeriod(
            string fundingStreamId,
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>> distinctTemplateMetadataFundingLinesContentsResponse =
                await _profilingApiClientPolicy.ExecuteAsync(() =>
                _profilingApiClient.GetProfilePatternsForFundingStreamAndFundingPeriod(
                    fundingStreamId, fundingPeriodId));

            return distinctTemplateMetadataFundingLinesContentsResponse?.Content;
        }

        private async Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel;
            try
            {
                providerProfilingResponseModel = await _profilingApiClient.GetProviderProfilePeriods(requestModel);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to Get Profile Periods on :' {requestModel}' from published providers.";

                _logger.Error(ex, errorMessage);

                throw new RetriableException(errorMessage, ex);
            }

            return providerProfilingResponseModel;
        }

        private void AddOrUpdateResponseDictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {  
            if (!dict.ContainsKey(key))
            {
                dict.Add(key,value);
            }
            else
            {
                dict = new Dictionary<TKey, TValue>
                {
                    { key, value }
                };
            }            
        }

        private void SaveFundingLineTotals(ref IEnumerable<FundingLine> fundingLineTotals,
             Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>> profilingResponses)
        {
            try
            {
                foreach (var paymentFundingLine in fundingLineTotals)
                {
                    if (profilingResponses.ContainsKey(paymentFundingLine.FundingLineCode))
                    {
                        List<DistributionPeriod> distributionPeriod = new List<DistributionPeriod>();
                        var rt = profilingResponses[paymentFundingLine.FundingLineCode][paymentFundingLine.Value];


                        foreach (var periods in rt.DistributionPeriods)
                        {
                            List<ProfilePeriod> profiles = (from profile in rt.DeliveryProfilePeriods.Where(r => r.DistributionPeriod == periods.DistributionPeriodCode)
                                                            select new ProfilePeriod()
                                                            {
                                                                Type = ProfilePeriodType.CalendarMonth,
                                                                TypeValue = profile.Period,
                                                                Year = profile.Year,
                                                                Occurrence = profile.Occurrence,
                                                                ProfiledValue = profile.Value,
                                                                DistributionPeriodId = profile.DistributionPeriod
                                                            }).ToList();

                            distributionPeriod.Add(new DistributionPeriod()
                            {
                                ProfilePeriods = profiles,
                                DistributionPeriodId = periods.DistributionPeriodCode,
                                Value = periods.Value
                            });

                        }
                        paymentFundingLine.DistributionPeriods = distributionPeriod;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save published profiling");

                throw;
            }
        }

        private bool AddOrUpdateDictionaryProfilingResponses<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            bool addedOrUpdated = false;
            if (!dict.TryGetValue(key, out TValue oldValue) || !value.Equals(oldValue))
            {
                dict[key] = value;
                addedOrUpdated = true;
            }
            return addedOrUpdated;
        }

        private ProviderProfilingRequestModel ConstructProfilingRequest(string fundingPeriodId,
            string fundingStreamId, 
            string fundingLineCodes, 
            decimal? fundingValue, 
            string profilePatternKey = null, 
            string providerType = null, 
            string providerSubType = null)
        {
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                FundingLineCode = fundingLineCodes,
                FundingValue = fundingValue,
                ProfilePatternKey = profilePatternKey,
                ProviderType = providerType,
                ProviderSubType = providerSubType
            };

            return requestModel;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _profilingApiClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProfilingService)
            };
            health.Dependencies.Add(new DependencyHealth 
            { 
                HealthOk = Ok, 
                DependencyName = _profilingApiClient.GetType().GetFriendlyName(), 
                Message = Message 
            });

            return health;
        }
    }
}
