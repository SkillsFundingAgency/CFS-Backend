using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class ProfilingService : IProfilingService
    {
        protected readonly IProfilingApiClient _profilingApiClient;
        private readonly ILogger _logger;

        public ProfilingService(ILogger logger, IProfilingApiClient profilingApiClient)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(profilingApiClient, nameof(profilingApiClient));
            _logger = logger;
            _profilingApiClient = profilingApiClient;
        }

        /// <summary>
        /// Profile funding lines
        /// </summary>
        /// <param name="fundingLineTotals">Funding lines for a specification</param>
        /// <param name="fundingStreamId">Funding Stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <returns></returns>
        public async Task ProfileFundingLines(IEnumerable<FundingLine> fundingLineTotals, string fundingStreamId, string fundingPeriodId)
        {
            Guard.ArgumentNotNull(fundingLineTotals, nameof(fundingLineTotals));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            Dictionary<decimal?, ProviderProfilingResponseModel> response =
                  new Dictionary<decimal?, ProviderProfilingResponseModel>();

            Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>> profilingResponses =
                new Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>>();

            if (fundingLineTotals.IsNullOrEmpty())
            {
                throw new ArgumentNullException($"Null or empty publsihed profiling fundingLineTotals.");
            }

            var fundingValues = fundingLineTotals
                .Select(k => new { FundingLineCode = k.FundingLineCode, Value = k.Value })
                .Distinct()
                .ToList();

            if (fundingValues.IsNullOrEmpty() || fundingValues.Count == 0)
            {
                string errorMessage = "No Funding Values of Type Payment in the Funding Totals for updating.";

                _logger.Error(errorMessage);

                return;
            }
                          
            foreach (var value in fundingValues)
            {
                ProviderProfilingRequestModel requestModel = ConstructProfilingRequest(fundingPeriodId,
                                                                fundingStreamId,
                                                                value.FundingLineCode,
                                                                value.Value);
                   
                ValidatedApiResponse<ProviderProfilingResponseModel> publishedProvider = await GetProviderProfilePeriods(requestModel);
                    
                if (publishedProvider?.Content == null)
                {
                    string errorMessage = $"Failed to Get Profile Periods for updating for Requested FundingPeriodId: '{fundingPeriodId}' and FundingStreamId: '{fundingStreamId}'";

                    _logger.Error(errorMessage);

                    throw new NonRetriableException(errorMessage);
                }

                AddOrUpdateResponseDictionary(ref response, value.Value, publishedProvider.Content);

                AddOrUpdateDictionaryProfilingResponses(profilingResponses, value.FundingLineCode, response);
            }
                
            SaveFundingLineTotals(ref fundingLineTotals, profilingResponses);
        }

        private async Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel = null;

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
                dict = new Dictionary<TKey, TValue>();
                dict.Add(key, value);                
            }            
        }

        private void SaveFundingLineTotals(ref IEnumerable<FundingLine> fundingLineTotals,
             Dictionary<string, Dictionary<decimal?, ProviderProfilingResponseModel>> profilingResponses)
        {
            try
            {
                foreach (var paymentFundingLine in fundingLineTotals)
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
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save published profiling");

                throw;
            }
        }

        private List<string> GetPaymentFundingLineCodes(
            Dictionary<string, IEnumerable<FundingLine>> fundingLineTotals)
        {
            var fundingLineCodes = fundingLineTotals.First().Value
                .Where(x => x.Type == OrganisationGroupingReason.Payment)
                .Select(x => x.FundingLineCode)
                .Distinct()
                .ToList();

            return fundingLineCodes;
        }

        private bool AddOrUpdateDictionaryProfilingResponses<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            bool addedOrUpdated = false;
            TValue oldValue;
            if (!dict.TryGetValue(key, out oldValue) || !value.Equals(oldValue))
            {
                dict[key] = value;
                addedOrUpdated = true;
            }
            return addedOrUpdated;
        }

        private ProviderProfilingRequestModel ConstructProfilingRequest(string fundingPeriodId,string fundingStreamId, string fundingLineCodes, decimal? fundingValue)
        {
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                FundingLineCode = fundingLineCodes,
                FundingValue = fundingValue
            };

            return requestModel;
        }
    }
}
