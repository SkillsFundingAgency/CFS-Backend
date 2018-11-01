using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    public class DatasetAggregationsRepository : IDatasetAggregationsRepository
    {
        private readonly IDatasetsApiClientProxy _datasetsApiClientProxy;

        public DatasetAggregationsRepository(IDatasetsApiClientProxy datasetsApiClientProxy)
        {
            _datasetsApiClientProxy = datasetsApiClientProxy;
        }

        public Task<IEnumerable<DatasetAggregations>> GetDatasetAggregationsForSpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"datasets/{specificationId}/datasetAggregations";

            return _datasetsApiClientProxy.GetAsync<IEnumerable<DatasetAggregations>>(url);
        }
    }
}
