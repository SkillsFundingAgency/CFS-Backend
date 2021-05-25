using System;
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
        private const string MultiplePredecessors = "Multiple predecessors";
        private const string OneOfMultipleSuccessors = "One of multiple successors";
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

        public async Task<IEnumerable<ProviderConverterDetail>> GetEligibleConvertersForProviderVersion(string providerVersionId,
            FundingConfiguration fundingConfiguration)
        {
            return await GetConvertersForProviderVersion(providerVersionId, fundingConfiguration, (_) => _.IsEligible);
        }

        public async Task<IEnumerable<ProviderConverterDetail>> GetConvertersForProviderVersion(string providerVersionId,
            FundingConfiguration fundingConfiguration,
            Func<ProviderConverterDetail, bool> predicate = null)
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

            IEnumerable<Provider> indicativeProviders = providerVersion.Providers?
                    .Where(_ => HasIndicativeStatus(indicativeStatusList, _));

            HashSet<string> notEligibleProviderIds = new HashSet<string>();

            return indicativeProviders.Select(_ => {
                ProviderConverterDetail providerConverter = new ProviderConverterDetail
                {
                    TargetProviderId = _.ProviderId,
                    TargetProviderName = _.Name,
                    TargetOpeningDate = _.DateOpened,
                    TargetStatus = _.Status,
                    PreviousProviderIdentifier = _.Predecessors.FirstOrDefault()
                };

                if (!HasSinglePredecessor(_))
                {
                    providerConverter.ProviderInEligible = MultiplePredecessors;
                }
                else
                {
                    string predecessorProviderId = _.Predecessors.Single();

                    if (IsForMerger(_.ProviderId, predecessorProviderId, allProvidersWithPredecessorsMap))
                    {
                        providerConverter.ProviderInEligible = OneOfMultipleSuccessors;
                    }
                }

                return providerConverter;
            }).Where(_ => predicate == null || predicate(_));
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