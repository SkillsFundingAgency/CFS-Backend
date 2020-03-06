using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;

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

        public async Task DeleteAllForSpecification(string specificationId)
        {
            await DeleteNodeAndChildNodes<Specification>(SpecificationId, specificationId);
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
    }
}
