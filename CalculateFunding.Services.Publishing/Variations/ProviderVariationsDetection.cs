using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class ProviderVariationsDetection : IDetectProviderVariations
    {
        private readonly IVariationStrategyServiceLocator _variationStrategyServiceLocator;
        private readonly IPoliciesService _policiesService;
        private readonly IProfilingService _profilingService;

        public ProviderVariationsDetection(IVariationStrategyServiceLocator variationStrategyServiceLocator,
            IPoliciesService policiesService,
            IProfilingService profilingService)
        {
            Guard.ArgumentNotNull(variationStrategyServiceLocator, nameof(variationStrategyServiceLocator));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));

            _policiesService = policiesService;
            _profilingService = profilingService;
            _variationStrategyServiceLocator = variationStrategyServiceLocator;
        }

        public async Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            decimal? updatedTotalFunding,
            Provider provider,
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapShots,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string providerVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData,
            string fundingStreamId,
            string fundingPeriodId,
            PublishedProviderVersion preRefreshProviderVersion,
            IEnumerable<string> variances)
        {
            Guard.ArgumentNotNull(existingPublishedProvider, nameof(existingPublishedProvider));
            Guard.ArgumentNotNull(updatedTotalFunding, nameof(updatedTotalFunding));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.ArgumentNotNull(variations, nameof(variations));
            Guard.ArgumentNotNull(allPublishedProviderRefreshStates, nameof(allPublishedProviderRefreshStates));
            Guard.ArgumentNotNull(allPublishedProviderSnapShots, nameof(allPublishedProviderSnapShots));

            FundingPeriod fundingPeriod = await _policiesService.GetFundingPeriodByConfigurationId(fundingPeriodId);
            IEnumerable<FundingStreamPeriodProfilePattern> profilePatterns = await _profilingService.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId);

            ProviderVariationContext providerVariationContext = new ProviderVariationContext(_policiesService)
            {
                PublishedProvider = existingPublishedProvider,
                UpdatedProvider = provider,
                UpdatedTotalFunding = updatedTotalFunding,
                AllPublishedProviderSnapShots = allPublishedProviderSnapShots,
                AllPublishedProvidersRefreshStates = allPublishedProviderRefreshStates,
                ProviderVersionId = providerVersionId,
                VariationPointers = variationPointers,
                OrganisationGroupResultsData = organisationGroupResultsData,
                ApplicableVariations = new List<string>(),
                Variances = variances,
                FundingPeriodStartDate = fundingPeriod.StartDate,
                FundingPeriodEndDate = fundingPeriod.EndDate,
                PreRefreshState = preRefreshProviderVersion,
                ProfilePatterns = profilePatterns?.ToDictionary(_ => string.IsNullOrWhiteSpace(_.ProfilePatternKey) ? _.FundingLineId : $"{_.FundingLineId}-{_.ProfilePatternKey}" )
            };

            foreach (FundingVariation configuredVariation in variations.OrderBy(_ => _.Order))
            {
                IVariationStrategy variationStrategy = _variationStrategyServiceLocator.GetService(configuredVariation.Name);
                
                bool stopSubsequentStrategies = await variationStrategy.Process(providerVariationContext, configuredVariation.FundingLineCodes);

                if (stopSubsequentStrategies)
                {
                    break;
                }
            }

            return providerVariationContext;
        }
    }
}
