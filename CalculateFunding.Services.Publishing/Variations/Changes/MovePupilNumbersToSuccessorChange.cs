using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using CalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MovePupilNumbersToSuccessorChange : VariationChange
    {
        public MovePupilNumbersToSuccessorChange(ProviderVariationContext variationContext, string strategyName)
            : base(variationContext, strategyName)
        {
        }

        protected override async Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            PublishedProviderVersion predecessor = RefreshState;
            string fundingStreamId = predecessor.FundingStreamId;
            string fundingPeriodId = predecessor.FundingPeriodId;
            string templateVersion = predecessor.TemplateVersion;

            IEnumerable<uint> pupilNumberTemplateCalculationIds = await new PupilNumberCalculationIdProvider(variationsApplications)
                .GetPupilNumberCalculationIds(fundingStreamId, fundingPeriodId, templateVersion);

            if (pupilNumberTemplateCalculationIds == null)
            {
                throw new InvalidOperationException("Cannot move pupil numbers to successor.\n" +
                                 $"Could not locate any pupil number template calculation ids for funding stream id:{fundingStreamId}, funding period id:{fundingPeriodId} and template version:{templateVersion}.");
            }

            Dictionary<uint, FundingCalculation> predecessorCalculations = predecessor
                .Calculations
                .ToDictionary(_ => _.TemplateCalculationId);

            SuccessorRefreshState.Calculations = SuccessorRefreshState.Calculations.AnyWithNullCheck() ?
                    SuccessorRefreshState.Calculations :
                    pupilNumberTemplateCalculationIds?.Select(_ => new FundingCalculation { TemplateCalculationId = _ });

            Dictionary<uint, FundingCalculation> successorCalculations = SuccessorRefreshState.Calculations.ToDictionary(_ => _.TemplateCalculationId);

            foreach (uint templateCalculationId in pupilNumberTemplateCalculationIds)
            {
                if (!predecessorCalculations.TryGetValue(templateCalculationId, out FundingCalculation predecessorCalculation) ||
                    !successorCalculations.TryGetValue(templateCalculationId, out FundingCalculation successorCalculation))
                {
                    throw new InvalidOperationException("Cannot move pupil numbers to successor.\n" +
                                  $"Could not locate both FundingCalculations for id {templateCalculationId}");
                }

                successorCalculation.Value = AddValueIfNotNull(successorCalculation.Value, predecessorCalculation.Value);
            }
        }

        public static int? AddValueIfNotNull(object value1, object value2)
        {
            if (value1 == null)
            {
                if (value2 == null)
                {
                    return null;
                }

                return Convert.ToInt32(value2);
            }

            if (value2 == null)
                return Convert.ToInt32(value1);

            return Convert.ToInt32(value2) + Convert.ToInt32(value1);
        }

        private class PupilNumberCalculationIdProvider
        {
            private readonly AsyncPolicy _policiesResilience;
            private readonly AsyncPolicy _cachingResilience;
            private readonly ICacheProvider _caching;
            private readonly IPoliciesApiClient _policies;

            public PupilNumberCalculationIdProvider(IApplyProviderVariations variationsApplication)
            {
                IPublishingResiliencePolicies resiliencePolicies = variationsApplication.ResiliencePolicies;

                Guard.ArgumentNotNull(variationsApplication.CacheProvider, nameof(variationsApplication.CacheProvider));
                Guard.ArgumentNotNull(variationsApplication.PoliciesApiClient, nameof(variationsApplication.PoliciesApiClient));
                Guard.ArgumentNotNull(resiliencePolicies?.CacheProvider, nameof(resiliencePolicies.CacheProvider));
                Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));

                _policiesResilience = resiliencePolicies.PoliciesApiClient;
                _cachingResilience = resiliencePolicies.CacheProvider;
                _policies = variationsApplication.PoliciesApiClient;
                _caching = variationsApplication.CacheProvider;
            }

            public async Task<IEnumerable<uint>> GetPupilNumberCalculationIds(string fundingStreamId,
                string fundingPeriodId,
                string templateVersion)
            {
                string cacheKey = new PupilNumberCalculationIdCacheKey(fundingStreamId, fundingPeriodId, templateVersion);

                if (!await _cachingResilience.ExecuteAsync(() => _caching.KeyExists<List<uint>>(cacheKey)))
                {
                    return await TryCachePupilNumberTemplateCalculationIds(cacheKey,
                         fundingStreamId,
                         fundingPeriodId,
                         templateVersion);
                }

                return await _cachingResilience.ExecuteAsync(() => _caching.GetAsync<List<uint>>(cacheKey));
            }

            private async Task<List<uint>> TryCachePupilNumberTemplateCalculationIds(string cacheKey,
                string fundingStreamId, string fundingPeriodId,
                string templateVersion)
            {
                ApiResponse<TemplateMetadataContents> templateContentsResponse = await _policiesResilience.ExecuteAsync(
                    () => _policies.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion));

                if (!templateContentsResponse.StatusCode.IsSuccess())
                {
                    throw new InvalidOperationException("Cannot move pupil numbers to successor.\n" +
                                                   $" Did not locate Template MetaData Contents for funding stream id {fundingStreamId} and template version {templateVersion}");
                }

                TemplateMetadataContents templateMetadataContents = templateContentsResponse.Content;

                IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines
                    .Flatten(_ => _.FundingLines) ?? new FundingLine[0];

                List<uint> pupilNumberTemplateCalculationIds = flattenedFundingLines.SelectMany(_ =>
                        _.Calculations.Flatten(cal => cal.Calculations))
                    .Where(_ => _.Type == CalculationType.PupilNumber)
                    .Select(_ => _.TemplateCalculationId)
                    .Distinct()
                    .ToList();

                await _cachingResilience.ExecuteAsync(() => _caching.SetAsync(cacheKey,
                    pupilNumberTemplateCalculationIds,
                    TimeSpan.FromHours(24),
                    false));

                return pupilNumberTemplateCalculationIds;
            }

            private class PupilNumberCalculationIdCacheKey
            {
                public PupilNumberCalculationIdCacheKey(string fundingStreamId, string fundingPeriodId, string templateVersion)
                {
                    Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
                    Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));
                    Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

                    Value = $"{CacheKeys.PupilNumberTemplateCalculationIds}{fundingStreamId}:{fundingPeriodId}:{templateVersion}";
                }

                private string Value { get; }

                public static implicit operator string(PupilNumberCalculationIdCacheKey key)
                {
                    return key.Value;
                }
            }
        }
    }
}