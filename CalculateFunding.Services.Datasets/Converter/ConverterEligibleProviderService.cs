using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterEligibleProviderService : IConverterEligibleProviderService
    {
        private readonly IProvidersApiClient _providers;
        private AsyncPolicy _providersResilience;

        public ConverterEligibleProviderService(IProvidersApiClient providers,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(providers, nameof(providers));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(_providersResilience));

            _providers = providers;
            _providersResilience = resiliencePolicies.ProvidersApiClient;
        }

        public async Task<IEnumerable<EligibleConverter>> GetEligibleConvertersForProviderVersion(string providerVersionId,
            FundingConfiguration fundingConfiguration)
        {
            EnsureIsNotNullOrWhitespace(providerVersionId, "Must supply a provider version id to query eligible providers");
            EnsureIsNotNull(fundingConfiguration, "Must supply a funding configuration to query eligible providers");

            ProviderVersion providerVersion = (await _providersResilience.ExecuteAsync(() =>
                    _providers.GetProvidersByVersion(providerVersionId)))?
                .Content;

            EnsureIsNotNull(providerVersion,
                $"Unable to get provider ids for converters. Could not locate provider version {providerVersionId}");

            HashSet<string> indicativeStatusList = fundingConfiguration.IndicativeOpenerProviderStatus?
                .Select(_ => _.ToLowerInvariant())
                .ToHashSet();

            Ensure(indicativeStatusList.AnyWithNullCheck(),
                "The funding configuration needs a list of indicative status labels to query eligible providers");

            Dictionary<string, HashSet<string>> allProvidersWithPredecessorsMap =
                providerVersion.Providers?
                    .Where(_ => _.Predecessors.AnyWithNullCheck())
                    .ToDictionary(_ => _.ProviderId, _ => _.Predecessors.ToHashSet())
                ?? new Dictionary<string, HashSet<string>>();

            Dictionary<string, Provider> indicativeProvidersWithSinglePredecessor =
                providerVersion.Providers?
                    .Where(_ => HasIndicativeStatus(indicativeStatusList, _) &&
                               HasSinglePredecessor(_))
                    .ToDictionary(_ => _.ProviderId)
                ?? new Dictionary<string, Provider>();

            HashSet<string> notEligibleProviderIds = new HashSet<string>();

            foreach ((string providerId, Provider value) in indicativeProvidersWithSinglePredecessor)
            {
                string predecessorProviderId = value.Predecessors.Single();

                if (IsForMerger(providerId, predecessorProviderId, allProvidersWithPredecessorsMap))
                {
                    notEligibleProviderIds.Add(providerId);
                }
            }

            return indicativeProvidersWithSinglePredecessor
                .Where(_ => !notEligibleProviderIds.Contains(_.Key))
                .Select(_ => new EligibleConverter
                {
                    ProviderId = _.Key,
                    PreviousProviderIdentifier = _.Value.Predecessors.Single()
                });
        }

        private static bool HasIndicativeStatus(ICollection<string> indicativeStatusList,
            Provider provider) =>
            indicativeStatusList.Contains(provider.Status.ToLower());

        private static bool HasSinglePredecessor(Provider provider)
            => provider.Predecessors?.Count() == 1;

        private static bool IsForMerger(string providerId,
            string predecessorProviderId,
            IDictionary<string, HashSet<string>> allProvidersWithPredecessorsMap) =>
            allProvidersWithPredecessorsMap.Where(_ => _.Key != providerId)
                .Any(_ => _.Value.Contains(predecessorProviderId));
    }
}