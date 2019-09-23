using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationsRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
