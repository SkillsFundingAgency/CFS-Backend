using CalculateFunding.Models.Calcs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsRepository
    {
        Task<HttpStatusCode> CreateDraftCalculation(Calculation calculation);

        Task<Calculation> GetCalculationById(string calculationId);

	    Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId);

		Task<IEnumerable<CalculationVersion>> GetVersionHistory(string calculationId);

        Task<IEnumerable<CalculationVersion>> GetCalculationVersions(CalculationVersionsCompareModel compareModel);

        Task<HttpStatusCode> UpdateCalculation(Calculation calculation);

        Task<IEnumerable<Calculation>> GetAllCalculations();

        Task UpdateCalculations(IEnumerable<Calculation> calculations);
    }
}
