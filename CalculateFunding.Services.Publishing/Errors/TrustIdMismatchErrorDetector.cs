using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class TrustIdMismatchErrorDetector : PublishedProviderErrorDetector
    {
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;
        private readonly IMapper _mapper;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly AsyncPolicy _publishingResiliencePolicy;

        public override string Name => nameof(TrustIdMismatchErrorDetector);

        public TrustIdMismatchErrorDetector(IOrganisationGroupGenerator organisationGroupGenerator,
            IMapper mapper,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));

            _organisationGroupGenerator = organisationGroupGenerator;
            _mapper = mapper;
            _publishedFundingDataService = publishedFundingDataService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
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

            IEnumerable<Common.ApiClient.Providers.Models.Provider> apiClientProviders = 
                _mapper.Map<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(publishedProvidersContext.ScopedProviders);

            static string OrganisationGroupsKey(string fundingStreamId, string fundingPeriodId) => 
                $"{fundingStreamId}:{fundingPeriodId}";
            
            IEnumerable<OrganisationGroupResult> organisationGroups;
            HashSet<string> organisationGroupsHashSet;
            string keyForOrganisationGroups = OrganisationGroupsKey(publishedProvider.Current.FundingStreamId, publishedProvider.Current.FundingPeriodId);

            if (publishedProvidersContext.OrganisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
            {
                organisationGroupsHashSet = publishedProvidersContext.OrganisationGroupResultsData[keyForOrganisationGroups];
            }
            else
            {
                organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(
                    publishedProvidersContext.FundingConfiguration, 
                    apiClientProviders, 
                    publishedProvidersContext.ProviderVersionId);
                organisationGroupsHashSet = organisationGroups.SelectMany(_ => _.Identifiers.Select(_ => $"{_.Type}-{_.Value}")).Distinct().ToHashSet();
                publishedProvidersContext.OrganisationGroupResultsData.Add(keyForOrganisationGroups, organisationGroupsHashSet);
            }

            publishedProvidersContext.CurrentPublishedFunding
                    .ForEach(x => {
                        if (organisationGroupsHashSet.Contains($"{x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue}")
                            && !x.Current.ProviderFundings.Any(pv => pv == publishedProvider.Released.FundingId))
                        {
                            errorCheck.AddError(new PublishedProviderError
                            {
                                Type = PublishedProviderErrorType.TrustIdMismatch,
                                Identifier = $"{x.Current.OrganisationGroupTypeIdentifier}-{x.Current.OrganisationGroupIdentifierValue}",
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
