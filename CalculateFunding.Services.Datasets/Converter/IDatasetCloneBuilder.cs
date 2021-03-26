using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using Polly;

namespace CalculateFunding.Services.Datasets.Converter
{
    public interface IDatasetCloneBuilder
    {
        Task<RowCopyResult> CopyRow(string sourceProviderId, string destinationProviderId);

        Task<IEnumerable<string>> GetExistingIdentifierValues(string fieldOfIdentifier);

        Task LoadOriginalDataset(Dataset dataset);

        Task<DatasetVersion> SaveContents(Common.Models.Reference author);
        IBlobClient Blobs { get; }
        AsyncPolicy BlobsResilience { get; }
    }
}