using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;


namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IGraphRepository
    {
        Task<IEnumerable<CalculationEntity>> GetCircularDependencies(string specificationId);        
        Task PersistToGraph(IEnumerable<Calculation> calculations, SpecificationSummary specification, string calculationId = null, bool withDelete = false, IEnumerable<DatasetReference> datasetReferences = null);
    }
}
