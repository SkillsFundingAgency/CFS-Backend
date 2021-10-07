using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IGenerateVariationReasonsForChannelService
    {
        Task<IDictionary<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>>> GenerateVariationReasonsForProviders(IEnumerable<string> providerIds,
                                                                                                                                        Channel channel,
                                                                                                                                        SpecificationSummary specification,
                                                                                                                                        FundingConfiguration fundingConfiguration,
                                                                                                                                        IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResults);
    }
}