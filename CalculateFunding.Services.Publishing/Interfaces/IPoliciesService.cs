using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPoliciesService
    {
        Task<FundingConfiguration> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId);
        Task<FundingPeriod> GetFundingPeriodByConfigurationId(string fundingPeriodConfigId);
        Task<TemplateMetadataContents> GetTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateId);
        Task<FundingDate> GetFundingDate(
            string fundingStreamId,
            string fundingPeriodId,
            string fundingLineId);
        Task<IEnumerable<FundingStream>> GetFundingStreams();
        Task<TemplateMetadataDistinctFundingLinesContents> GetDistinctTemplateMetadataFundingLinesContents(
            string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IEnumerable<PublishedFundingTemplate>> GetFundingTemplates(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<string>> GetDistinctFundingLineNames(string fundingStreamId, string fundingPeriodId);
    }
}
