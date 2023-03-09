using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using ProfilePatternKey = CalculateFunding.Models.Publishing.ProfilePatternKey;

namespace CalculateFunding.Services.Publishing
{
    public class BatchProfilingContext : PagedContext<BatchProfilingRequestModel>, IBatchProfilingContext
    {
        private readonly IDictionary<string, GeneratedProviderResult> _generatedPublishedProviderData;

        public BatchProfilingContext(IDictionary<string, GeneratedProviderResult> generatedPublishedProviderData) : base(null)
        {
            Guard.ArgumentNotNull(generatedPublishedProviderData, nameof(generatedPublishedProviderData));

            _generatedPublishedProviderData = generatedPublishedProviderData;
        }

        public ICollection<ProviderProfilingRequestData> ProfilingRequests { get; set; } = new List<ProviderProfilingRequestData>();

        public IDictionary<string, ProfilingBatch> ProfilingBatches { get; set; }

        public void AddProviderProfilingRequestData(PublishedProvider publishedProvider,
            IDictionary<string, GeneratedProviderResult> generatedProviderResults,
            bool isNewProvider)
        {
            PublishedProviderVersion providerVersion = publishedProvider.Current;
            PublishedProviderVersion releasedProviderVersion = publishedProvider.Released;
            Provider provider = providerVersion.Provider;
            
            // only profile a value if not null and it's not equal to zero
            // OR previous released version contains value
            FundingLine[] fundingLines = generatedProviderResults[provider.ProviderId]
                .FundingLines?
                .Where(_ => _.Type == FundingLineType.Payment
                            && 
                            _.Value.HasValue 
                            && 
                                (_.Value != 0 ||
                                ((releasedProviderVersion?.FundingLines?.Any(pfl => pfl.Type == FundingLineType.Payment && pfl.FundingLineCode == _.FundingLineCode && pfl.Value != null && pfl.Value != 0)) ?? false))
                            && 
                            !providerVersion.FundingLineHasCustomProfile(_.FundingLineCode))
                .ToArray();

            // add all funding lines which were previously either null payment on released or weren't included in last release
            HashSet<string> newInScopeFundingLines = fundingLines?
                .Where(fl => providerVersion.FundingLines == null || providerVersion
                                    .FundingLines
                                    .Any(_ => _.Type == FundingLineType.Payment &&
                                            (_.Value.HasValue ? _.Value == 0 : true) &&
                                            _.FundingLineCode == fl.FundingLineCode) ||
                                providerVersion.FundingLines
                                    .All(_ => _.FundingLineCode != fl.FundingLineCode)).Select(_ => _.FundingLineCode).ToHashSet();

            if (fundingLines != null)
            {
                ProfilingRequests.Add(new ProviderProfilingRequestData
                {
                    FundingLinesToProfile = fundingLines,
                    NewInScopeFundingLines = isNewProvider ? null : newInScopeFundingLines,
                    PublishedProvider = providerVersion,
                    ProviderType = isNewProvider ? provider.ProviderType : null,
                    ProviderSubType = isNewProvider ? provider.ProviderSubType : null,
                    ProfilePatternKeys = isNewProvider ? null : providerVersion.ProfilePatternKeys?
                        .ToDictionary(_ => _.FundingLineCode, _ => _.Key) ?? new Dictionary<string, string>(),
                });
            }
        }

        public void InitialiseItems(int pageSize,
            int batchSize)
        {
            InitialiseProfilingBatches();
            InitialiseItems(new BatchProfilingRequestModels(ProfilingBatches.Values, batchSize), pageSize);       
        }

        private void InitialiseProfilingBatches()
        {
            ProfilingBatches ??= Enumerable.DistinctBy(new ProfilingBatches(ProfilingRequests), _ => _.Key).ToDictionary(_ => _.Key);
        }

        public void ReconcileBatchProfilingResponse(BatchProfilingResponseModel response)
        {
            if (ProfilingBatches == null || 
                !ProfilingBatches.TryGetValue(response.Key, out ProfilingBatch batch))
            {
                throw new NonRetriableException(
                    $"Unable to reconcile profiling response. Could not locate batch for response {response.Key}");
            }

            UpdateFundingLineDistributionPeriods(response, batch, _generatedPublishedProviderData);
            EnsurePublishedProvidersHaveProfilePatternDetails(response, batch);
        }

        private static void EnsurePublishedProvidersHaveProfilePatternDetails(BatchProfilingResponseModel response,
            ProfilingBatch batch)
        {
            ProfilePatternKey profilePatternKey = new ProfilePatternKey
            {
                Key = response.ProfilePatternKey,
                FundingLineCode = batch.FundingLineCode
            };

            foreach (PublishedProviderVersion publishedProviderVersion in batch.PublishedProviders)
            {
                lock (publishedProviderVersion)
                {
                    publishedProviderVersion.SetProfilePatternKey(profilePatternKey);
                }
            }
        }

        private static void UpdateFundingLineDistributionPeriods(BatchProfilingResponseModel response,
             ProfilingBatch batch,
            IDictionary<string, GeneratedProviderResult> generatedPublishedProviderData)
        {
            if (response.ProfilePatternType == ProfilePatternType.Calculation)
            {
                foreach (PublishedProviderVersion publishedProvider in batch.PublishedProviders)
                {
                    GeneratedProviderResult generatedProviderResult = generatedPublishedProviderData[publishedProvider.ProviderId];

                    DistributionPeriod[] publishedProviderDistributionPeriods = response.DistributionPeriods.Select(_ => new DistributionPeriod
                    {
                        DistributionPeriodId = _.DistributionPeriodCode,
                        Value = _.Value,
                        ProfilePeriods = response.DeliveryProfilePeriods.Where(pp =>
                                pp.DistributionPeriod == _.DistributionPeriodCode)
                    .Select(pp => new ProfilePeriod
                    {
                        Occurrence = pp.Occurrence,
                        Type = pp.Type.AsEnum<ProfilePeriodType>(),
                        Year = pp.Year,
                        TypeValue = pp.Period,
                        ProfiledValue = Convert.ToDecimal(generatedPublishedProviderData[publishedProvider.ProviderId].Calculations?.FirstOrDefault(_ => _.TemplateCalculationId == pp.CalculationId)?.Value ?? 0),
                        DistributionPeriodId = _.DistributionPeriodCode
                    })
                    .ToArray()
                    }).ToArray();

                    FundingLine fundingLine = generatedProviderResult.FundingLines.FirstOrDefault(_ => _.FundingLineCode == batch.FundingLineCode);
                    if (fundingLine != null)
                    {
                        fundingLine.DistributionPeriods = publishedProviderDistributionPeriods.DeepCopy();
                    }
                }

                return;
            }

            DistributionPeriod[] distributionPeriods = response.DistributionPeriods.Select(_ => new DistributionPeriod
            {
                DistributionPeriodId = _.DistributionPeriodCode,
                Value = _.Value,
                ProfilePeriods = response.DeliveryProfilePeriods.Where(pp =>
                        pp.DistributionPeriod == _.DistributionPeriodCode)
                    .Select(pp => new ProfilePeriod
                    {
                        Occurrence = pp.Occurrence,
                        Type = pp.Type.AsEnum<ProfilePeriodType>(),
                        Year = pp.Year,
                        TypeValue = pp.Period,
                        ProfiledValue = pp.Value,
                        DistributionPeriodId = _.DistributionPeriodCode
                    })
                    .ToArray()
            }).ToArray();

            foreach (FundingLine fundingLine in batch.FundingLines)
            {
                fundingLine.DistributionPeriods = distributionPeriods.DeepCopy();
            }
        }
    }
}