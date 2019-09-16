using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Providers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedSearchService
    {
        Task<IActionResult> SearchPublishedProviders(HttpRequest request);
    }
}
