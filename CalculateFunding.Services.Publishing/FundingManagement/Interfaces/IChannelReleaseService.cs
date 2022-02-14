using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IChannelReleaseService
    {
        Task ReleaseProvidersForChannel(Channel channel,
                                        FundingConfiguration fundingConfiguration,
                                        SpecificationSummary specification,
                                        IEnumerable<string> batchPublishedProviderIds,
                                        Reference author,
                                        string jobId,
                                        string correlationId);
    }
}
