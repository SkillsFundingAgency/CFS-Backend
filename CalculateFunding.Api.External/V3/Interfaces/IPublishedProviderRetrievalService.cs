using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IPublishedProviderRetrievalService
    {
        Task<IActionResult> GetPublishedProviderInformation(string publishedProviderVersion);
    }
}
