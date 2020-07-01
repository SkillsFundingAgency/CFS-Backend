using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationIndexer : IHealthChecker
    {
        Task Index(Specification specification);
        
        Task Index(IEnumerable<Specification> specifications);
        
        Task Index(IEnumerable<SpecificationSearchModel> specifications);
    }
}