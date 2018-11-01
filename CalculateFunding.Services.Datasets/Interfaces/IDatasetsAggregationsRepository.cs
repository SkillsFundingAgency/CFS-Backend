using CalculateFunding.Models.Datasets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetsAggregationsRepository
    {
        Task CreateDatasetAggregations(DatasetAggregations datasetAggregations);

        Task<IEnumerable<DatasetAggregations>> GetDatasetAggregationsForSpecificationId(string specificationId);
    }
}
