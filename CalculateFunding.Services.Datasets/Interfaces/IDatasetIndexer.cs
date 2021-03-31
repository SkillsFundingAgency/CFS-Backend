using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetIndexer
    {
        Task IndexDatasetAndVersion(Dataset dataset);
    }
}