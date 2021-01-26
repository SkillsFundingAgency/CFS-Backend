using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateService
    {
        Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, string template, string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<string>> GetFundingTemplateSourceFile(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<TemplateMetadataContents>> GetFundingTemplateContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<FundingTemplateContents>> GetFundingTemplate(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<bool> TemplateExists(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<IEnumerable<PublishedFundingTemplate>>> GetFundingTemplates(string fundingStreamId, string fundingPeriodId);
        Task<ActionResult<TemplateMetadataDistinctContents>> GetDistinctFundingTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<TemplateMetadataDistinctFundingLinesContents>> GetDistinctFundingTemplateMetadataFundingLinesContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<TemplateMetadataDistinctCalculationsContents>> GetDistinctFundingTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<ActionResult<TemplateMetadataFundingLineCashCalculationsContents>> GetCashCalcsForTemplateVersion(string fundingStreamId, string fundingPeriodId, string templateVersion);
    }
}
