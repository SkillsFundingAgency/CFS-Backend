using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class DatasetCloneBuilder : IDatasetCloneBuilder
    {
        public DatasetCloneBuilder(IBlobClient blobs,
            IDatasetRepository datasets,
            IExcelDatasetReader reader,
            IExcelDatasetWriter writer,
            IDatasetIndexer indexer,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Blobs = blobs;
            Reader = reader;
            Writer = writer;
            Indexer = indexer;
            Datasets = datasets;
            BlobsResilience = resiliencePolicies.BlobClient;
            DatasetsResilience = resiliencePolicies.DatasetRepository;
        }
        
        public IDatasetIndexer Indexer { get; }
        
        public IExcelDatasetReader Reader { get; }
        
        public IExcelDatasetWriter Writer { get; }

        public IBlobClient Blobs { get; }
        
        public IDatasetRepository Datasets { get; }

        public AsyncPolicy BlobsResilience { get; }
        
        public AsyncPolicy DatasetsResilience { get; }

        public IEnumerable<TableLoadResult> DatasetData { get; set; }

        public RowCopyResult CopyRow(string fieldNameOfIdentifier, string sourceProviderId, string destinationProviderId)
        {
            TableLoadResult datasetTable = DatasetData.First();

            RowLoadResult sourceRow = datasetTable.GetRowWithMatchingFieldValue(fieldNameOfIdentifier, sourceProviderId);

            EligibleConverter eligibleConverter = new EligibleConverter
            {
                PreviousProviderIdentifier = sourceProviderId,
                ProviderId = destinationProviderId
            };
            
            if (sourceRow == null)
            {
                return new RowCopyResult
                {
                    Outcome = RowCopyOutcome.SourceRowNotFound,
                    EligibleConverter = eligibleConverter
                };
            }

            RowLoadResult destinationRow = datasetTable.GetRowWithMatchingFieldValue(fieldNameOfIdentifier, destinationProviderId);

            if (destinationRow != null)
            {
                return new RowCopyResult
                {
                    Outcome = RowCopyOutcome.DestinationRowAlreadyExists,
                    EligibleConverter = eligibleConverter
                };
            }

            destinationRow = sourceRow.CopyRow(destinationProviderId, 
                IdentifierFieldType.UKPRN, 
                (fieldNameOfIdentifier, destinationProviderId));
            
            datasetTable.Rows.Add(destinationRow);

            return new RowCopyResult
            {
                Outcome = RowCopyOutcome.Copied,
                EligibleConverter = eligibleConverter
            };
        }

        public IEnumerable<string> GetExistingIdentifierValues(string identifierFieldName) =>
            DatasetData?
                .FirstOrDefault()?
                .Rows
                .Where(_ => _.Fields.ContainsKey(identifierFieldName))
                .Select(_ => _.Fields[identifierFieldName]?.ToString())
                .Distinct()
                .ToArray();

        public async Task LoadOriginalDataset(Dataset dataset,
            DatasetDefinition datasetDefinition)
        {
            EnsureIsNotNull(dataset, "No dataset supplied to load excel blob data from.");
            EnsureIsNotNull(datasetDefinition, "No dataset definition supplied to load excel blob data from.");

            string blobPath = GetDatasetVersionExcelBlobName(dataset);

            ICloudBlob blob = await BlobsResilience.ExecuteAsync(() => Blobs.GetBlobReferenceFromServerAsync(blobPath));

            EnsureIsNotNull(blob, $"No blob located with path {blobPath}");

            (blobPath, blob) = await GetExcelBlob(dataset);

            await using Stream excelStream = await BlobsResilience.ExecuteAsync(() => Blobs.DownloadToStreamAsync(blob));

            Ensure(excelStream?.Length > 0, $"Blob {blob.Name} contains no data.");

            DatasetData = Reader.Read(excelStream, datasetDefinition);
            
            EnsureIsNotNull(DatasetData?.FirstOrDefault(), $"No dataset table located for xls {blobPath}");
        }

        private static string GetDatasetVersionExcelBlobName(Dataset dataset)
        {
            return $"{dataset.Id}/v{dataset.Current?.Version}/{dataset.Current?.BlobName}";
        }

        public async Task<DatasetVersion> SaveContents(Reference author, 
            DatasetDefinition datasetDefinition, 
            Dataset dataset)
        {
            int rowCount = (DatasetData?.FirstOrDefault()?.Rows?.Count).GetValueOrDefault();
            
            await CreateNewDatasetVersion(dataset, author, rowCount);
            
            byte[] excelData = Writer.Write(datasetDefinition, DatasetData);

            await using MemoryStream excelStream = new MemoryStream(excelData);
            await UploadBlob(dataset, datasetDefinition, author, excelStream);
            
            return dataset.Current;
        }

        private async Task CreateNewDatasetVersion(Dataset dataset,
            Reference author,
            int rowCount)
        {
            dataset.CreateNewVersion(author, rowCount, DatasetChangeType.ConverterWizard);

            HttpStatusCode statusCode = await DatasetsResilience.ExecuteAsync(() => Datasets.SaveDataset(dataset));

            Ensure(statusCode.IsSuccess(), $"Failed to save new dataset {dataset.Id}");

            await Indexer.IndexDatasetAndVersion(dataset);
        }

        private async Task UploadBlob(Dataset dataset,
            DatasetDefinition datasetDefinition,
            Reference author,
            Stream excelStream)
        {
            string blobPath = GetDatasetVersionExcelBlobName(dataset);

            ICloudBlob blob = Blobs.GetBlockBlobReference(blobPath);

            await BlobsResilience.ExecuteAsync(() => blob.UploadFromStreamAsync(excelStream));
            
            blob.Metadata["dataDefinitionId"] = datasetDefinition.Id;
            blob.Metadata["datasetId"] = dataset.Id;
            blob.Metadata["authorId"] = author.Id;
            blob.Metadata["authorName"] = author.Name;
            blob.Metadata["name"] = dataset.Current.BlobName;
            blob.Metadata["description"] = dataset.Description;
            blob.Metadata["fundingStreamId"] = datasetDefinition.FundingStreamId;
            blob.Metadata["converterWizard"] = true.ToString();
            
            blob.SetMetadata();
        }

        private async Task<(string path, ICloudBlob blob)> GetExcelBlob(Dataset dataset)
        {
            string blobPath = GetDatasetVersionExcelBlobName(dataset);

            ICloudBlob blob = await BlobsResilience.ExecuteAsync(() => Blobs.GetBlobReferenceFromServerAsync(blobPath));

            EnsureIsNotNull(blob, $"No blob located with path {blobPath}");

            return (blobPath, blob);
        }
    }
}