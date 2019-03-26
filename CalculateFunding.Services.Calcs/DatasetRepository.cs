using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class DatasetRepository : IDatasetRepository
    {
        private readonly IDatasetsApiClientProxy _datasetsApiClientProxy;

        public DatasetRepository(IDatasetsApiClientProxy datasetsApiClientProxy)
        {
            _datasetsApiClientProxy = datasetsApiClientProxy;
        }

        public async Task<IEnumerable<DatasetSchemaRelationshipModel>> GetDatasetSchemaRelationshipModelsForSpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string url = $"datasets/{specificationId}/schemaRelationshipFields";

            return await _datasetsApiClientProxy.GetAsync<IEnumerable<DatasetSchemaRelationshipModel>>(url);
        }

        public async Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetCurrentRelationshipsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string url = $"datasets/get-relationships-by-specificationId?specificationId={specificationId}";

            return await _datasetsApiClientProxy.GetAsync<IEnumerable<DatasetSpecificationRelationshipViewModel>>(url);
        }

        public async Task<DatasetDefinition> GetDatasetDefinitionById(string datasetDefinitionId)
        {
            Guard.IsNullOrWhiteSpace(datasetDefinitionId, nameof(datasetDefinitionId));

            string url = $"datasets/get-dataset-definition-by-id?datasetDefinitionId={datasetDefinitionId}";

            return await _datasetsApiClientProxy.GetAsync<DatasetDefinition>(url);
        }
    }
}