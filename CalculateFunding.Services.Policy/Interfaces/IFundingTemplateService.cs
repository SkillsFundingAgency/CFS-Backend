using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateService
    {
        Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, string template, string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetFundingTemplateSourceFile(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetFundingTemplateContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetFundingTemplate(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<bool> TemplateExists(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetFundingTemplates(string fundingStreamId, string fundingPeriodId);
        Task<IActionResult> GetDistinctFundingTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetDistinctFundingTemplateMetadataFundingLinesContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
        Task<IActionResult> GetDistinctFundingTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion);
    }
}
