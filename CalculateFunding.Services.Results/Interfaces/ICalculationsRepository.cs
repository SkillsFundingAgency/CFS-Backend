using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<Calculation> GetCalculationById(string calculationId);
    }
}
