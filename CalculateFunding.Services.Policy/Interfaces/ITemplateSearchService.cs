using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.Search;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface ITemplateSearchService
    {
        Task<IActionResult> SearchTemplates(SearchModel searchModel);
        Task<IActionResult> ReIndex(Reference user, string correlationId);
        Task<Job> CreateReIndexJob(Reference user, string correlationId);
    }
}