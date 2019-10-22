using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<Calculation> GetCalculationById(string calculationId);
    }
}
