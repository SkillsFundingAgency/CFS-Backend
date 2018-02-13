using CalculateFunding.Models.Specs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<Specification> GetSpecificationById(string specificationId);
    }
}
