using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class ExcelBlobContext : TrackedDataSource<BlobIdentity>
    {
        protected BlobContainerClient BlobContainerClient;

        protected ExcelBlobContext(IConfiguration configuration,
            string blobContainerName)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));

            IConfigurationSection storageConfiguration = configuration.GetSection("CommonStorageSettings");

            Guard.ArgumentNotNull(storageConfiguration, nameof(storageConfiguration));

            string blobStoreUri = storageConfiguration["ConnectionString"];

            Guard.IsNullOrWhiteSpace(blobStoreUri, nameof(blobStoreUri));
            Guard.IsNullOrWhiteSpace(blobContainerName, nameof(blobContainerName));


            BlobServiceClient blobServiceClient = new BlobServiceClient(blobStoreUri);

            BlobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
        }

        public async Task CreateContextData(params dynamic[] documentData)
        {
            int count = documentData.Length;

            List<ImportStream> temporaryDocuments = new List<ImportStream>(count);
            List<BlobIdentity> batchIdentities = new List<BlobIdentity>(count);

            foreach (dynamic data in documentData)
            {
                ExcelDocument document = GetExcelDocument(data);

                CreateImportStream(document, batchIdentities, temporaryDocuments);
            }

            await RemoveData(batchIdentities);
            await InsertContextData(temporaryDocuments);
        }

        protected void CreateImportStream(ExcelDocument document,
            List<BlobIdentity> batchIdentities,
            List<ImportStream> temporaryDocuments)
        {
            BlobIdentity blobIdentity = new BlobIdentity(document.Path);

            ImportedDocuments.Add(blobIdentity);
            batchIdentities.Add(blobIdentity);

            MemoryStream documentStream = new MemoryStream();

            document.Document.SaveAs(documentStream);

            temporaryDocuments.Add(ImportStream.ForBlob(documentStream, blobIdentity.Name));
        }

        protected abstract ExcelDocument GetExcelDocument(dynamic documentData);

        protected ExcelPackage CreateExcelFile(ExcelWorksheetData worksheetData)
        {
            ExcelPackage excelPackage = new ExcelPackage();

            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add(worksheetData.Name);

            string[] headers = worksheetData.Headers;

            for (int column = 0; column < headers.Length; column++)
            {
                worksheet.Cells[1, column + 1].Value = headers[column];
            }

            IEnumerable<object[]> rows = worksheetData.Rows;

            for (int row = 0; row < rows.Count(); row++)
            {
                object[] rowData = rows.ElementAt(row);

                Ensure(rowData.Length == headers.Length,
                    $"Row {row} has column length mismatch with headers expected {headers.Length} but was {rowData.Length}");

                for (int column = 0; column < headers.Length; column++)
                {
                    worksheet.Cells[row + 2, column + 1].Value = rowData[column];
                }
            }

            return excelPackage;
        }

        protected override void RunImportTask(ImportStream importStream)
        {
            MemoryStream cosmosStream = new MemoryStream((int) importStream.Stream.Length);
            importStream.Stream.CopyToAsync(cosmosStream)
                .Wait();
            importStream.Stream.Position = 0;

            string blobName = importStream.Id;

            BlobContentInfo blobInfo = BlobContainerClient.UploadBlobAsync(blobName, importStream.Stream)
                .GetAwaiter()
                .GetResult();

            importStream.Stream.Dispose();

            string failedMessage = $"Failed to insert excel document to blob store for {blobName}";

            bool requestSucceeded = blobInfo != null;

            TraceInformation(requestSucceeded
                ? $"Inserted json document to blob store for {blobName}"
                : failedMessage);

            ThrowExceptionIfRequestFailed(requestSucceeded, failedMessage);
        }

        protected override void RunRemoveTask(BlobIdentity documentIdentity)
        {
            string blobName = documentIdentity.Name;

            Response<bool> blobResponse = BlobContainerClient.DeleteBlobIfExistsAsync(blobName)
                .GetAwaiter()
                .GetResult();

            bool requestSucceeded = blobResponse.Value;

            string failedMessage = $"Failed to delete blob store excel document {blobName}";

            TraceInformation(requestSucceeded
                ? $"Deleted blob store excel document {blobName}"
                : failedMessage);
        }

        public async Task<ExcelPackage> GetExcelDocument(string path, bool csvContent = false)
        {
            ExcelPackage excelPackage = new ExcelPackage();

            BlobClient blob = BlobContainerClient.GetBlobClient(path);

            Ensure(await blob.ExistsAsync(), $"Expected {path} to exist");
            
            Response<BlobDownloadInfo> response = await blob.DownloadAsync();

            await using MemoryStream stream = new MemoryStream();

            await response.Value.Content.CopyToAsync(stream);

            stream.Seek(0, SeekOrigin.Begin);

            if (csvContent)
            {
                ExcelWorksheet ws = excelPackage.Workbook.Worksheets.Add("Sheet1");
                ExcelTextFormat format = new ExcelTextFormat()
                {
                    Delimiter = ','
                };
                ws.Cells[1, 1].LoadFromText(new StreamReader(stream).ReadToEnd(), format);
            }
            else
            {
                excelPackage.Load(stream);
            }

            await response.Value.Content.DisposeAsync();

            return excelPackage;
        }

        protected override void PerformExtraCleanUp()
        {
            RemoveContextData()
                .Wait();
        }
    }
}