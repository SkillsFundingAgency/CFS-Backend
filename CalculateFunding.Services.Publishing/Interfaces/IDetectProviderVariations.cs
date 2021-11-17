using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDetectProviderVariations
    {
        Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            decimal? totalFunding,
            Provider provider,
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapShots,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string providerVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData,
            string fundingPeriodId,
            IEnumerable<string> variances = null);
    }
}
