using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsSearchService
    {
        Task<IActionResult> SearchSpecifications(HttpRequest request);
    }
}
