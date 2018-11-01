using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calcs.Interfaces;
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

        public Task<IEnumerable<DatasetSchemaRelationshipModel>> GetDatasetSchemaRelationshipModelsForSpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"datasets/{specificationId}/schemaRelationshipFields";

            return _datasetsApiClientProxy.GetAsync<IEnumerable<DatasetSchemaRelationshipModel>>(url);
        }
    }
}
