using CalculateFunding.Models.Aggregations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IDatasetAggregationsRepository
    {
        Task<IEnumerable<DatasetAggregation>> GetDatasetAggregationsForSpecificationId(string specificationId);
    }
}
