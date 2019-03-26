using CalculateFunding.Models.Aggregations;
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

        Task<HttpStatusCode> UpdateCalculation(Calculation calculation);

        Task<IEnumerable<Calculation>> GetAllCalculations();

        Task UpdateCalculations(IEnumerable<Calculation> calculations);

        Task<Calculation> GetCalculationByCalculationSpecificationId(string calculationSpecificationId);

        Task<StatusCounts> GetStatusCounts(string specificationId);

        Task<CompilerOptions> GetCompilerOptions(string specificationId);
    }
}
