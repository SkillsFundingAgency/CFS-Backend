using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateService
    {
        Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, HttpRequest request);

        Task<IActionResult> GetFundingTemplate(string fundingStreamId, string templateVersion);
    }
}
