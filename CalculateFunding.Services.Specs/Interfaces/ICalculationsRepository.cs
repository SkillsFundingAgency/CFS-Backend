using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<bool> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId = null);
    }
}