﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosService
    {
        Task<IActionResult> SaveVersion(HttpRequest request);

        Task<IActionResult> GetTestScenariosBySpecificationId(HttpRequest request);
    }
}
