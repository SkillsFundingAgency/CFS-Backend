using CalculateFunding.Models.Specs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ISpecificationRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
