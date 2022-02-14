using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface IResultsRepository
    {
        Task<bool> SpecificationHasResults(string specificationId);
    }
}
