using CalculateFunding.Models.Specs;
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
    }
}
