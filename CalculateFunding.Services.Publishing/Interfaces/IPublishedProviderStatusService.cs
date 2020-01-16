using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStatusService
    {
        Task<IActionResult> GetProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status);
    }
}
