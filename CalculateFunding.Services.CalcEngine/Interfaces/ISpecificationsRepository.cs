using CalculateFunding.Models.Specs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
