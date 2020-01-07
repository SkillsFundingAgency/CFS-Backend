using CalculateFunding.Models.Aggregations;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
{
    public class DatasetAggregationsRepository : IDatasetAggregationsRepository
    {
        private readonly IDatasetsApiClientProxy _datasetsApiClientProxy;

        public DatasetAggregationsRepository(IDatasetsApiClientProxy datasetsApiClientProxy)
        {
            _datasetsApiClientProxy = datasetsApiClientProxy;
        }

        public Task<IEnumerable<DatasetAggregation>> GetDatasetAggregationsForSpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"datasets/{specificationId}/datasetAggregations";

            return _datasetsApiClientProxy.GetAsync<IEnumerable<DatasetAggregation>>(url);
        }
    }
}
