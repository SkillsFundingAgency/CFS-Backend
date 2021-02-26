using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class TrustIdMismatchErrorDetector : PublishedProviderErrorDetector
    {
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IMapper _mapper;

        public override string Name => nameof(TrustIdMismatchErrorDetector);

        public TrustIdMismatchErrorDetector(IOrganisationGroupGenerator organisationGroupGenerator,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _organisationGroupGenerator = organisationGroupGenerator;
            _mapper = mapper;
        }

        protected override void ClearErrors(PublishedProviderVersion publishedProviderVersion)
        {
            publishedProviderVersion.Errors?.RemoveAll(_ => _.Type == PublishedProviderErrorType.TrustIdMismatch);
        }

        protected override async Task<ErrorCheck> HasErrors(
            PublishedProvider publishedProvider,
            PublishedProvidersContext publishedProvidersContext)
        {
            Guard.ArgumentNotNull(publishedProvidersContext, nameof(publishedProvidersContext));

            ErrorCheck errorCheck = new ErrorCheck();

            // if there is no released version then we don't need to do the check
            if (publishedProvider.Released == null)
            {
                return errorCheck;
            }

            IEnumerable<Provider> apiClientProviders =
                _mapper.Map<IEnumerable<Provider>>(publishedProvidersContext.ScopedProviders);

            static string OrganisationGroupsKey(string fundingStreamId,
                string fundingPeriodId)
            {
                return $"{fundingStreamId}:{fundingPeriodId}";
            }

            IEnumerable<OrganisationGroupResult> organisationGroups;

            string keyForOrganisationGroups = OrganisationGroupsKey(publishedProvider.Current.FundingStreamId, publishedProvider.Current.FundingPeriodId);

            if (publishedProvidersContext.OrganisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
            {
                organisationGroups = publishedProvidersContext.OrganisationGroupResultsData[keyForOrganisationGroups];
            }
            else
            {
                organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(
                    publishedProvidersContext.FundingConfiguration,
                    apiClientProviders,
                    publishedProvidersContext.ProviderVersionId);
                publishedProvidersContext.OrganisationGroupResultsData.Add(keyForOrganisationGroups, organisationGroups);
            }

            HashSet<string> organisationGroupsHashSet = organisationGroups
                                            .Where(_ => _.Providers.AnyWithNullCheck()
                                                        && _.Providers.Any(p => p.ProviderId == publishedProvider.Current.ProviderId))
                                            .SelectMany(_ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}")).Distinct().ToHashSet();

            publishedProvidersContext.CurrentPublishedFunding
                .ForEach(x =>
                {
                    if (organisationGroupsHashSet.Contains($"{x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue}")
                        && x.Current.ProviderFundings.All(pv => pv != publishedProvider.Released.FundingId))
                    {
                        errorCheck.AddError(new PublishedProviderError
                        {
                            Type = PublishedProviderErrorType.TrustIdMismatch,
                            DetailedErrorMessage = $"TrustId {x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue} not matched.",
                            SummaryErrorMessage = "TrustId  not matched",
                            FundingStreamId = publishedProvider.Current.FundingStreamId
                        });
                    }
                });

            return errorCheck;
        }
    }
}