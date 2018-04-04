using CalculateFunding.Models.Specs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ISpecificationRepository
    {
        Task<Specification> GetSpecificationById(string specificationId);
    }
}
