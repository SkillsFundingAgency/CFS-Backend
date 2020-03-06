using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface ISpecificationRepository
    {
        Task DeleteSpecification(string specificationId);

        Task UpsertSpecifications(IEnumerable<Specification> specifications);
        
        Task DeleteAllForSpecification(string specificationId);
        
        Task CreateSpecificationDatasetRelationship(string specificationId, string datasetId);
        
        Task DeleteSpecificationDatasetRelationship(string specificationId, string datasetId);
    }
}
