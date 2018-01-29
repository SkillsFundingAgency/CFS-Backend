﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(Message message);

        Task<IActionResult> GetCalculationById(HttpRequest request);

        Task<IActionResult> GetCalculationVersions(HttpRequest request);

        Task<IActionResult> GetCalculationHistory(HttpRequest request);

        Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request);

        Task<IActionResult> SaveCalculationVersion(HttpRequest request);
    }
}
