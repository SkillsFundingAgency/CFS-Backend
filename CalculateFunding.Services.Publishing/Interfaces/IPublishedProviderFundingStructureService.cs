using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderFundingStructureService
    {
        Task<IActionResult> GetPublishedProviderFundingStructure(string publishedProviderVersionId);

        Task<IActionResult> GetCurrentPublishedProviderFundingStructure(string specificationId,
            string fundingStreamId,
            string providerId);
    }
}
