using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(Message message);

        Task<IActionResult> SearchCalculations(HttpRequest request);

        Task<IActionResult> GetCalculationById(HttpRequest request);
    }
}
