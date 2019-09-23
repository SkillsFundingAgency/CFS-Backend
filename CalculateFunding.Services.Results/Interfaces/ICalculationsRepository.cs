using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Results.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ICalculationsRepository
    {
        Task<Calculation> GetCalculationById(string calculationId);
    }
}
