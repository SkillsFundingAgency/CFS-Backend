using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using System.Linq;

namespace CalculateFunding.Services.Graph
{
    public class SpecificationRepository : GraphRepositoryBase, ISpecificationRepository
    {
        private const string SpecificationId = "specificationid";
        
        public const string SpecificationDatasetRelationship = "ReferencesDataset";
        public const string DatasetSpecificationRelationship = "IsReferencedBySpecification";

        public SpecificationRepository(IGraphRepository graphRepository)
        : base(graphRepository)
        {
        }

        public async Task DeleteSpecification(string specificationId)
        {
            await DeleteNode<Specification>(SpecificationId, specificationId);
        }

        public async Task UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            await UpsertNodes(specifications, SpecificationId);
        }
        
        public async Task CreateSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            await UpsertRelationship<Specification, Dataset>(SpecificationDatasetRelationship,
                (SpecificationId, specificationId),
                (Dataset.IdField, datasetId));
            
            await UpsertRelationship<Dataset, Specification>(DatasetSpecificationRelationship,
                (Dataset.IdField, datasetId),
                (SpecificationId, specificationId));
        }
        
        public async Task DeleteSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            await DeleteRelationship<Specification, Dataset>(SpecificationDatasetRelationship,
                (SpecificationId, specificationId),
                (Dataset.IdField, datasetId));
            
            await DeleteRelationship<Dataset, Specification>(DatasetSpecificationRelationship,
                (Dataset.IdField, datasetId),
                (SpecificationId, specificationId));
        }

        public async Task<IEnumerable<Entity<Specification, IRelationship>>> GetAllEntities(string specificationId)
        {
            IEnumerable<Entity<Specification>> entities = await GetAllEntities<Specification>(SpecificationId,
                specificationId,
                new[] { CalculationRepository.CalculationACalculationBRelationship,
                    CalculationRepository.CalculationSpecificationRelationship
                });
            return entities.Select(_ => new Entity<Specification, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}
