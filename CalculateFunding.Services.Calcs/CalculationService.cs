using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService
    {
        public Task CreateCalculation(Message message)
        {
            return Task.CompletedTask;
        }
    }
}
