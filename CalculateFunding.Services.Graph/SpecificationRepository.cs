using System;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using System.Linq;
using CalculateFunding.Services.Graph.Constants;

namespace CalculateFunding.Services.Graph
{
    public class SpecificationRepository : GraphRepositoryBase, ISpecificationRepository
    {
        public SpecificationRepository(IGraphRepository graphRepository)
        : base(graphRepository)
        {
        }

        public async Task DeleteSpecification(string specificationId)
        {
            await DeleteNode<Specification>(AttributeConstants.SpecificationId, specificationId);
        }
        
        public async Task DeleteSpecifications(params string[] specificationIds)
        {
            await DeleteNodes<Specification>( specificationIds.Select(_ =>  (AttributeConstants.SpecificationId, _)).ToArray());
        }

        public async Task UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            await UpsertNodes(specifications, AttributeConstants.SpecificationId);
        }
        
        public async Task CreateSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            await UpsertRelationship<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.DatasetId, datasetId));
            
            await UpsertRelationship<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.SpecificationId, specificationId));
        }

        public async Task CreateSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships)
        {
            await UpsertRelationships<Specification, Dataset>(relationships.Select(_ =>  (AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, _.specificationId),
                (AttributeConstants.DatasetId, _.datasetId)))
                .ToArray());
            
            await UpsertRelationships<Dataset, Specification>(relationships.Select(_ => (AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, _.datasetId),
                (AttributeConstants.SpecificationId, _.specificationId)))
                .ToArray());
        }
        
        public async Task DeleteSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            await DeleteRelationship<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.DatasetId, datasetId));
            
            await DeleteRelationship<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.SpecificationId, specificationId));
        }
        
        public async Task DeleteSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships)
        {
            await DeleteRelationships<Specification, Dataset>( relationships.Select(_ =>  (AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, _.specificationId),
                (AttributeConstants.DatasetId, _.datasetId)))
                .ToArray());
            
            await DeleteRelationships<Dataset, Specification>(relationships.Select(_ => (AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, _.datasetId),
                (AttributeConstants.SpecificationId, _.specificationId)))
                .ToArray());
        }

        public async Task<IEnumerable<Entity<Specification, IRelationship>>> GetAllEntities(string specificationId)
        {
            IEnumerable<Entity<Specification>> entities = await GetAllEntities<Specification>(AttributeConstants.SpecificationId,
                specificationId,
                new[] { AttributeConstants.CalculationACalculationBRelationship,
                    AttributeConstants.CalculationSpecificationRelationshipId
                });
            return entities.Select(_ => new Entity<Specification, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}
