using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderVersionService
    {
        Task<IActionResult> GetPublishedProviderVersionBody(string publishedProviderVersionId);

        Task SavePublishedProviderVersionBody(string publishedProviderVersionId, string publishedProviderVersionBody);
        
        Task<IActionResult> ReIndex(Reference user, string correlationId);
    }
}
