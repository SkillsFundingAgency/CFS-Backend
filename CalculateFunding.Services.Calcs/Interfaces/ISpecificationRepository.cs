using CalculateFunding.Models.Specs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISpecificationRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
        Task<HttpStatusCode> UpdateCalculationLastUpdatedDate(string specificationId);
        Task<IEnumerable<SpecificationSummary>> GetAllSpecificationSummaries();
    }
}
