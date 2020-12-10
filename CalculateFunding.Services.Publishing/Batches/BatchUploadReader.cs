using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
using OfficeOpenXml;
using Polly;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadReader : IBatchUploadReader
    {
        private const string ContainerName = "batchuploads";
        private const int PageSize = 100;

        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobResilience;

        private List<string> _ukprns = new List<string>();
        private int _page;
        private int _pageCount;

        public BatchUploadReader(IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            
            _blobClient = blobClient;
            _blobResilience = resiliencePolicies.BlobClient;
        }

        public async Task LoadBatchUpload(string blobName)
        {
            await using Stream batchStream = await BatchStream(blobName);
            
            batchStream.Position = 0;
            
            using ExcelPackage batchXlsx = new ExcelPackage(batchStream);
            
            LoadPublishedProviderIds(batchXlsx);
        }

        private void LoadPublishedProviderIds(ExcelPackage batchXlsx)
        {
            ExcelWorksheet batchSheet = batchXlsx.Workbook.Worksheets.FirstOrDefault();

            if (batchSheet == null)
            {
                throw new NonRetriableException("The batch upload must contain at least one worksheet");
            }

            ExcelAddressBase dimension = batchSheet.Dimension;
            ExcelCellAddress start = dimension.Start;
            ExcelCellAddress end = dimension.End;

            for (int column = start.Column; column <= end.Column; column++)
            {
                ExcelRange cell = batchSheet.Cells[start.Row, column];

                if (cell.GetValue<string>()?.ToLowerInvariant() == "ukprn")
                {
                    _ukprns = new List<string>();
                    
                    for (int row = start.Row + 1; row <= end.Row; row++)
                    {
                        ExcelRange ukprn = batchSheet.Cells[row, column];
                        
                        _ukprns.Add(ukprn.GetValue<string>()?.ToLowerInvariant());
                    }

                    _page = 0;
                    _pageCount = (int) Math.Ceiling(_ukprns.Count / (decimal)PageSize); 
                    
                    return;
                }
            }
            
            throw new NonRetriableException("Did not locate a ukprn column in batch upload file");
           
        }

        private async Task<Stream> BatchStream(string blobName)
        {
            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(blobName, ContainerName);
            
            return await _blobResilience.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));
        }

        public bool HasPages => _page < _pageCount;

        public int Count => _ukprns.Count;

        public IEnumerable<string> NextPage()
        {
            if (!HasPages)
            {
                throw new NonRetriableException("No more pages available to return");
            }

            return _ukprns.Skip(_page++ * PageSize).Take(PageSize);
        }
    }
}