using CalculateFunding.Models.Calcs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<Calculation> GetCalculationById(string calculationId);
    }
}
