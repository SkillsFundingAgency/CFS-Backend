using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public interface ICalculationNameInUseCheck
    {
        Task<bool?> IsCalculationNameInUse(string specificationId, string calculationName, string existingCalculationId);
    }
}