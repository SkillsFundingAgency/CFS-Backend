using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IAllocationsService
    {
        IActionResult GetAllocationByAllocationResultId(string allocationResultId, HttpRequest httpRequest);
    }
}
