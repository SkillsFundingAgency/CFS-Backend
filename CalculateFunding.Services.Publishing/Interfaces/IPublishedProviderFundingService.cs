using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderFundingService : IHealthChecker
    {
        Task<IActionResult> GetLatestPublishedProvidersForSpecificationId(string specificationId);
    }
}