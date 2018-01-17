using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(Calculation calculation);
    }
}
