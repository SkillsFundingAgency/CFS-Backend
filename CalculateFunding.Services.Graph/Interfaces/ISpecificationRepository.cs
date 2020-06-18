using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface ISpecificationRepository
    {
        Task DeleteSpecification(string specificationId);

        Task UpsertSpecifications(IEnumerable<Specification> specifications);
        
        Task CreateSpecificationDatasetRelationship(string specificationId, string datasetId);
        
        Task DeleteSpecificationDatasetRelationship(string specificationId, string datasetId);
        Task<IEnumerable<Entity<Specification, IRelationship>>> GetAllEntities(string specificationId);
    }
}
