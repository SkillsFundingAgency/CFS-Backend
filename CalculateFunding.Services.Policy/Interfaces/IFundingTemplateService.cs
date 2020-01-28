using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateService
    {
        Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, string template);
        Task<IActionResult> GetFundingTemplateSourceFile(string fundingStreamId, string templateVersion);
        Task<IActionResult> GetFundingTemplateContents(string fundingStreamId, string templateVersion);
        Task<IActionResult> GetFundingTemplate(string fundingStreamId, string templateVersion);
        Task<bool> TemplateExists(string fundingStreamId, string templateVersion);
    }
}
