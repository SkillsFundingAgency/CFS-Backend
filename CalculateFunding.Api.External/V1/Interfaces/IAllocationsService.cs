using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Interfaces
{
    public interface IAllocationsService
    {
        Task<IActionResult> GetAllocationByAllocationResultId(string allocationResultId, int? version, HttpRequest httpRequest);

        Task<IActionResult> GetAllocationAndHistoryByAllocationResultId(string allocationResultId, HttpRequest httpRequest);
    }
}
