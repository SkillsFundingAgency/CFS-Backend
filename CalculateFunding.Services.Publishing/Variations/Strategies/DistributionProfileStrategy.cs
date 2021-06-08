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
    public class DistributionProfileStrategy : Variation, IVariationStrategy
    {
        public string Name => "DistributionProfile";

        public Task<VariationStrategyResult> DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            if (providerVariationContext.ReleasedState == null)
            {
                return Task.FromResult(StrategyResult);
            }

            string keyForOrganisationGroups = ProviderVariationContext.OrganisationGroupsKey(providerVariationContext.ReleasedState.FundingStreamId, providerVariationContext.ReleasedState.FundingPeriodId);
            
            if(providerVariationContext.OrganisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
            {
                IEnumerable<OrganisationGroupResult> organisationGroups = providerVariationContext.OrganisationGroupResultsData[keyForOrganisationGroups];

                if(organisationGroups.Any(x => x.GroupReason == OrganisationGroupingReason.Contracting))
                {
                    providerVariationContext.AddVariationReasons(VariationReason.DistributionProfileUpdated);
                    providerVariationContext.QueueVariationChange(new SetProfilePeriodValuesChange(providerVariationContext));

                    // Stop subsequent strategies                    
                    StrategyResult.StopSubsequentStrategies = true;
                }
            }

            return Task.FromResult(StrategyResult);
        }
    }
}