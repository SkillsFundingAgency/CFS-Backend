using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class OrganisationGroupService : IOrganisationGroupService
    {
        private readonly IMapper _mapper;
        private readonly IOrganisationGroupGenerator _organisationGroupGenerator;

        public OrganisationGroupService(
            IMapper mapper,
            IOrganisationGroupGenerator organisationGroupGenerator)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(organisationGroupGenerator, nameof(organisationGroupGenerator));

            _mapper = mapper;
            _organisationGroupGenerator = organisationGroupGenerator;
        }

        public async Task<Dictionary<string, IEnumerable<OrganisationGroupResult>>> GenerateOrganisationGroups(
            IEnumerable<Provider> scopedProviders,
            IEnumerable<PublishedProvider> publishedProviders,
            FundingConfiguration fundingConfiguration,
            string providerVersionId,
            int? providerSnapshotId = null)
        {
            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData 
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>>();

            IEnumerable<Common.ApiClient.Providers.Models.Provider> apiClientProviders
                = _mapper.Map<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(scopedProviders);

            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                IEnumerable<OrganisationGroupResult> organisationGroups;

                string keyForOrganisationGroups = OrganisationGroupsKey(
                    publishedProvider.Current.FundingStreamId, 
                    publishedProvider.Current.FundingPeriodId);

                if (!organisationGroupResultsData.ContainsKey(keyForOrganisationGroups))
                {
                    organisationGroups = await _organisationGroupGenerator.GenerateOrganisationGroup(
                        fundingConfiguration,
                        apiClientProviders,
                        providerVersionId,
                        providerSnapshotId);

                    organisationGroupResultsData.Add(keyForOrganisationGroups, organisationGroups);
                }
            }

            return organisationGroupResultsData;
        }

        static string OrganisationGroupsKey(string fundingStreamId, string fundingPeriodId)
        {
            return $"{fundingStreamId}:{fundingPeriodId}";
        }
    }
}
