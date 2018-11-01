using CalculateFunding.Models.Datasets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IDatasetAggregationsRepository
    {
        Task<IEnumerable<DatasetAggregations>> GetDatasetAggregationsForSpecificationId(string specificationId);
    }
}
