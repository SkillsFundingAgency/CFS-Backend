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
        Task<IActionResult> GetAllocationAndHistoryByAllocationResultId(string allocationResultId, HttpRequest httpRequest);

        IActionResult GetAllocationByAllocationResultId(string allocationResultId, HttpRequest httpRequest);
    }
}
