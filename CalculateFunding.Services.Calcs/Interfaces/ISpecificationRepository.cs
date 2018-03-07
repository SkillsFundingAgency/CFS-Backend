using CalculateFunding.Models.Specs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ISpecificationRepository
    {
        Task<Specification> GetSpecificationById(string specificationId);
    }
}
