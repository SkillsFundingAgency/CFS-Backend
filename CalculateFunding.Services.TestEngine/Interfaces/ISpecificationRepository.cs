using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
