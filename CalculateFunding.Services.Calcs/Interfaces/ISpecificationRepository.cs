using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
        Task<HttpStatusCode> UpdateCalculationLastUpdatedDate(string specificationId);
        Task<IEnumerable<SpecificationSummary>> GetAllSpecificationSummaries();
    }
}
