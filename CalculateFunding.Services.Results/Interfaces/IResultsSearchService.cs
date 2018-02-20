using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IResultsSearchService
    {
        Task<IActionResult> SearchDatasets(HttpRequest request);
	    Task<IActionResult> GetProviderResults(HttpRequest httpContextRequest);
    }
}
