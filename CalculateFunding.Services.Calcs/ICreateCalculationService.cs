using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs
{
    public interface ICreateCalculationService
    {
        Task<CreateCalculationResponse> CreateCalculation(string specificationId,
            CalculationCreateModel model,
            CalculationNamespace calculationNamespace,
            CalculationType calculationType,
            Reference author,
            string correlationId, 
            bool initiateCalcRun = true);
    }
}