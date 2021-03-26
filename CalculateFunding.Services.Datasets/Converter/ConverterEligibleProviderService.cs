using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Models.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterEligibleProviderService : IConverterEligibleProviderService
    {
        private readonly IProvidersApiClient _providersApiClient;

        public ConverterEligibleProviderService(IProvidersApiClient providersApiClient)
        {
            _providersApiClient = providersApiClient;
        }

        public async Task<IEnumerable<EligibleConverter>> GetProviderIdsForConverters(string providerVersionId, FundingConfiguration fundingConfiguration)
        {
            ApiResponse<ProviderVersion> providerLookupResult = await _providersApiClient.GetProvidersByVersion(providerVersionId);

            List<Provider> providersWithPredecessors = new List<Provider>(providerLookupResult.Content.Providers.Where(_ => _.Predecessors.AnyWithNullCheck()));

            Dictionary<string, Provider> providersToRemove = new Dictionary<string, Provider>();

            // Check provider status matches indicative openers from funding configuration
            //             fundingConfiguration.IndicativeOpenerProviderStatus
            // Ensure match is case insensitive

            foreach (var provider in providersWithPredecessors)
            {
                foreach (var predecessor in provider.Predecessors)
                {
                    IEnumerable<Provider> otherProvidersWithSamePredecessors = providersWithPredecessors
                         .Where(_ => _.ProviderId != provider.ProviderId && provider.Predecessors.Union(_.Predecessors).Any());

                    if (otherProvidersWithSamePredecessors.Any())
                    {
                        foreach (var otherProvider in otherProvidersWithSamePredecessors)
                        {
                            providersToRemove[otherProvider.ProviderId] = otherProvider;
                        }

                        providersToRemove[provider.ProviderId] = provider;
                    }
                }

                if (provider.Predecessors.Count() > 1)
                {
                    providersToRemove[provider.ProviderId] = provider;
                }
            }

            return providersWithPredecessors.Except(providersToRemove.Values)
                .Select(_ => new EligibleConverter()
                {
                    ProviderId = _.ProviderId,
                    PreviousProviderIdentifier = _.Predecessors.First(),
                });
        }
    }
}