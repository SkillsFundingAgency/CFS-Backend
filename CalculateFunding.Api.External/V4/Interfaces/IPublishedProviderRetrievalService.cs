using CalculateFunding.Api.External.V4.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IPublishedProviderRetrievalService
    {
        Task<ActionResult<ProviderVersionSearchResult>> GetPublishedProviderInformation(string channel, string publishedProviderVersion);
    }
}
