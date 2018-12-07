using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IAllocationsService
    {
        Task<IActionResult> GetAllocationByAllocationResultId(string allocationResultId, int? version, HttpRequest httpRequest);

        Task<IActionResult> GetAllocationAndHistoryByAllocationResultId(string allocationResultId, HttpRequest httpRequest);
    }
}
