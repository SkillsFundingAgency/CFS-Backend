using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderFundingService
    {
        Task<IActionResult> GetLatestPublishedProvidersForSpecificationId(string specificationId);
    }
}