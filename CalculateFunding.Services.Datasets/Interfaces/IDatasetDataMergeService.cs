using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetDataMergeService
    {
        Task<DatasetDataMergeResult> Merge(DatasetDefinition datasetDefinition, string latestBlobFileName, string blobFileNameToMerge);
    }
}
