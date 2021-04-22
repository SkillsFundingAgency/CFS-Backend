using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderVersionService
    {
        Task<IActionResult> GetPublishedProviderVersionBody(string publishedProviderVersionId);

        Task SavePublishedProviderVersionBody(string publishedProviderVersionId, string publishedProviderVersionBody, string specificationId);

        Task<ActionResult<Job>> ReIndex(Reference user, string correlationId);

        Task<Job> CreateReIndexJob(Reference user, string correlationId, string specificationId = null, string parentJobId = null);
    }
}
