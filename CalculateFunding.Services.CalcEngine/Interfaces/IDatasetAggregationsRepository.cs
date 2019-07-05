using CalculateFunding.Models.Datasets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IDatasetAggregationsRepository
    {
        Task<IEnumerable<DatasetAggregations>> GetDatasetAggregationsForSpecificationId(string specificationId);
    }
}
