using System.Collections.Generic;
using System.Threading.Tasks;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IGraphRepository
    {
        Task<IEnumerable<CalculationEntity>> GetCircularDependencies(string specificationId);        
    }
}
