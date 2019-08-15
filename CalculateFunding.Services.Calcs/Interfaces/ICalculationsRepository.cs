using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;

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

        Task<StatusCounts> GetStatusCounts(string specificationId);

        Task<CompilerOptions> GetCompilerOptions(string specificationId);

        Task<Calculation> GetCalculationsBySpecificationIdAndCalculationName(string specificationId, string calculationName);

        Task<IEnumerable<CalculationMetadata>> GetCalculationsMetatadataBySpecificationId(string specificationId);

        Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId);

        Task UpdateTemplateMapping(string specificationId, string fundingStreamId, TemplateMapping templateMapping);

        Task<int> GetCountOfNonApprovedTemplateCalculations(string specificationId);
    }
}
