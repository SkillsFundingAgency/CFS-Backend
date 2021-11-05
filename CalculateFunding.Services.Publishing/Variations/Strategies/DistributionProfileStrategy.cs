using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class DistributionProfileStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "DistributionProfile";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            if (providerVariationContext.ReleasedState == null)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            string keyForOrganisationGroups = ProviderVariationContext.OrganisationGroupsKey(providerVariationContext.ReleasedState.FundingStreamId, providerVariationContext.ReleasedState.FundingPeriodId);
            bool stopSubsequentStrategies = false;

            if (providerVariationContext.OrganisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
            {
                IEnumerable<OrganisationGroupResult> organisationGroups = providerVariationContext.OrganisationGroupResultsData[keyForOrganisationGroups];

                if (organisationGroups.Any(x => x.GroupReason == OrganisationGroupingReason.Contracting && x.Providers.AnyWithNullCheck(_ => _.ProviderId == providerVariationContext.ProviderId)))
                {
                    // Stop subsequent strategies                    
                    stopSubsequentStrategies = true;
                }
            }

            return Task.FromResult(stopSubsequentStrategies);
        }
    }
}