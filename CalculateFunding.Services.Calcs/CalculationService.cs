
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
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
            Reference reference = message.GetUserDetails();
            Calculation calculation = message.GetPayloadAsInstanceOf<Calculation>();

            if (calculation == null)
                return Task.CompletedTask;

            return Task.CompletedTask;
        }
    }
}
