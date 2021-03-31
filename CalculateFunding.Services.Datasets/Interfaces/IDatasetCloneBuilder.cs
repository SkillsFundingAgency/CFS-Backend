using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using Polly;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetCloneBuilder
    {
        RowCopyResult CopyRow(string fieldNameOfIdentifier, string sourceProviderId, string destinationProviderId);

        IEnumerable<string> GetExistingIdentifierValues(string identifierFieldName);

        Task LoadOriginalDataset(Dataset dataset, DatasetDefinition datasetDefinition);
        
        IBlobClient Blobs { get; }
        
        AsyncPolicy BlobsResilience { get; }
        
        IExcelDatasetReader Reader { get; }
        
        IEnumerable<TableLoadResult> DatasetData { get; }
        IExcelDatasetWriter Writer { get; }
        IDatasetRepository Datasets { get; }
        AsyncPolicy DatasetsResilience { get; }

        Task<DatasetVersion> SaveContents(Reference author, 
            DatasetDefinition datasetDefinition, 
            Dataset dataset);
    }
}