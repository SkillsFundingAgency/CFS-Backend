using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.TemplateMetadata.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPoliciesService
    {
        Task<FundingConfiguration> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId);
        Task<FundingPeriod> GetFundingPeriodByConfigurationId(string fundingPeriodConfigId);
        Task<string> GetFundingPeriodId(string fundingPeriodConfigId);
        Task<TemplateMetadataContents> GetTemplateMetadataContents(string fundingStreamId, string templateId);
    }
}
