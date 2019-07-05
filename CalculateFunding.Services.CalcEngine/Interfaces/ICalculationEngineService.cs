﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngineService
    {
        Task GenerateAllocations(Message message);

        Task<IActionResult> GenerateAllocations(HttpRequest request);
    }
}
