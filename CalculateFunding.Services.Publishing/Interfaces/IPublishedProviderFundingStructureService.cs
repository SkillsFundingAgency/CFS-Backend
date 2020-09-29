using CalculateFunding.Models.Publishing;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderFundingStructureService
    {
        Task<IActionResult> GetPublishedProviderFundingStructure(string publishedProviderVersionId);
    }
}
