using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Models.Calcs.Calculation>;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IGraphRepository
    {
        Task<IEnumerable<CalculationEntity>> GetCircularDependencies(string specificationId);
        Task PersistToGraph(IEnumerable<Calculation> calculations, SpecificationSummary specification, string calculationId = null, bool withDelete = false);
    }
}
