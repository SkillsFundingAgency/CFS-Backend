using System.Collections.Generic;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IPolicyRepository
    {
        Task<IEnumerable<PoliciesApiModels.FundingStream>> GetFundingStreams();
        Task<PoliciesApiModels.FundingStream> GetFundingStream(string fundingStreamId);
        Task<PoliciesApiModels.FundingPeriod> GetFundingPeriod(string fundingPeriodId);
        Task<PoliciesApiModels.TemplateMetadataDistinctCalculationsContents> GetDistinctTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion);

        Task<PoliciesApiModels.TemplateMetadataDistinctContents> GetDistinctTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
    }
}
