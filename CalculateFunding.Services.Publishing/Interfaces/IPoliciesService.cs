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
        Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId);
        Task<TemplateMetadataContents> GetTemplateMetadataContents(string fundingStreamId, string templateId);
    }
}
