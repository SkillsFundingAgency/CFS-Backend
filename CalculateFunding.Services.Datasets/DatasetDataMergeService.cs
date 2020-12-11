using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetDataMergeService : IDatasetDataMergeService
    {
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IExcelDatasetReader _excelDatasetReader;
        private readonly IExcelDatasetWriter _excelDatasetWriter;
        private readonly AsyncPolicy _blobClientPolicy;

        public DatasetDataMergeService(
            IBlobClient blobClient,
            ILogger logger,
            IExcelDatasetReader excelDatasetReader,
            IExcelDatasetWriter excelDatasetWriter,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(excelDatasetReader, nameof(excelDatasetReader));
            Guard.ArgumentNotNull(excelDatasetWriter, nameof(excelDatasetWriter));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.BlobClient, nameof(datasetsResiliencePolicies.BlobClient));

            _blobClient = blobClient;
            _logger = logger;
            _excelDatasetReader = excelDatasetReader;
            _excelDatasetWriter = excelDatasetWriter;
            _blobClientPolicy = datasetsResiliencePolicies.BlobClient;
        }

        public async Task<DatasetDataMergeResult> Merge(DatasetDefinition datasetDefinition,
            string latestBlobFileName,
            string blobFileNameToMerge)
        {
            DatasetDataMergeResult result = new DatasetDataMergeResult();
            
            bool success;
            string errorMessage;
            List<TableLoadResult> latestTableLoadResults;
            List<TableLoadResult> tableLoadResultsToMerge;

            (success, errorMessage, latestTableLoadResults) = await ReadExcelDatasetData(datasetDefinition, latestBlobFileName);
            if (!success)
            {
                result.ErrorMessage = errorMessage;
                _logger.Error(errorMessage);
                return result;
            }

            (success, errorMessage, tableLoadResultsToMerge) = await ReadExcelDatasetData(datasetDefinition, blobFileNameToMerge);
            if (!success)
            {
                result.ErrorMessage = errorMessage;
                _logger.Error(errorMessage);
                return result;
            }

            foreach (TableLoadResult latestTableLoadResult in latestTableLoadResults)
            {
                TableLoadResult tableLoadResultToMerge = tableLoadResultsToMerge.FirstOrDefault(x => x.TableDefinition?.Name == latestTableLoadResult.TableDefinition.Name);

                if (tableLoadResultToMerge == null || !tableLoadResultToMerge.Rows.Any())
                {
                    result.TablesMergeResults.Add(new DatasetDataTableMergeResult
                    {
                        TableDefinitionName = latestTableLoadResult.TableDefinition.Name
                    });
                }
                else
                {
                    // Merge updates latestTableLoadResult with tableLoadResultToMerge data
                    result.TablesMergeResults.Add(Merge(latestTableLoadResult, tableLoadResultToMerge));
                }
            }

            if (result.HasChanges)
            {
                // NOTE: If any new / updated rows after merge (rows merged into latest (previous version) dataset), then the merge file will be replaced with latest merge data. 
                byte[] excelAsBytes = _excelDatasetWriter.Write(datasetDefinition, latestTableLoadResults);

                ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(blobFileNameToMerge);

                try
                {
                    await using MemoryStream memoryStream = new MemoryStream(excelAsBytes);
                    await _blobClientPolicy.ExecuteAsync(() => blob.UploadFromStreamAsync(memoryStream));
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"Failed to upload {datasetDefinition.Name} to blob storage after merge.";
                    _logger.Error(ex, result.ErrorMessage);
                }
            }

            return result;
        }

        private async Task<(bool success, string errorMessage, List<TableLoadResult> tableLoadResults)> ReadExcelDatasetData(DatasetDefinition datasetDefinition,
            string blobFileName)
        {
            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(blobFileName);

            if (blob == null)
            {
                return (false, $"Failed to find blob with path: {blobFileName}", new List<TableLoadResult>());
            }

            await using Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob);

            if (datasetStream == null || datasetStream.Length == 0)
            {
                return (false, $"Blob {blob.Name} contains no data", new List<TableLoadResult>());
            }

            List<TableLoadResult> tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
            return (true, null, tableLoadResults);
        }

        private DatasetDataTableMergeResult Merge(TableLoadResult latestTableLoadResult,
            TableLoadResult tableLoadResultToMerge)
        {
            IEnumerable<RowLoadResult> newRows = tableLoadResultToMerge.GetRowsMissingFrom(latestTableLoadResult).ToArray();
            IEnumerable<RowLoadResult> updatedRows = tableLoadResultToMerge.GetRowsWhereFieldsDifferFromMatchIn(latestTableLoadResult).ToArray();

            latestTableLoadResult.UpdateMatchingRowsWithFieldsValuesFrom(updatedRows);
            latestTableLoadResult.AddRows(newRows);

            return new DatasetDataTableMergeResult
            {
                TableDefinitionName = latestTableLoadResult.TableDefinition.Name,
                NewRowsCount = newRows.Count(),
                UpdatedRowsCount = updatedRows.Count()
            };
        }
    }
}