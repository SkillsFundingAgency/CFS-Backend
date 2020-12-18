using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class BatchUploadReaderTests
    {
        private Mock<IBlobClient> _blobClient;

        private BatchUploadReader _reader;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = new Mock<IBlobClient>();
            
            _reader = new BatchUploadReader(_blobClient.Object,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task LoadsAndPagesUkprnsFromExcelFileInBlobStorage()
        {
            /*
             *  Embedded resource has ukprns 10000143-10000441
             */
            string batchId = NewRandomString();
            
            GivenTheUploadedExcelFile(batchId, "batch_1.xlsx");

            await WhenTheBatchUploadIsLoaded(batchId);
            
            List<string> actualUkprns = new List<string>();

            while (_reader.HasPages)
            {
                actualUkprns.AddRange(_reader.NextPage());
            }

            actualUkprns.ToArray()
                .Should()
                .BeEquivalentTo(Enumerable.Range(10000143, 299).Select(_ => _.ToString()));
        }

        [TestMethod]
        public void GuardsAgainstMissingUkprnColumnInExcelWorkbook()
        {
            string batchId = NewRandomString();
            
            GivenTheUploadedExcelFile(batchId, "batch_2.xlsx");

            Action invocation = () => WhenTheBatchUploadIsLoaded(batchId)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Did not locate a ukprn column in batch upload file");
        }

        [TestMethod]
        public void GuardsAgainstInvalidFileFormatInSuppliedStream()
        {
            string batchId = NewRandomString();
            
            GivenTheUploadedExcelFile(batchId, new MemoryStream(new RandomBytes()));

            Action invocation = () => WhenTheBatchUploadIsLoaded(batchId)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to open batch upload file. It must be a valid xlsx file.");   
        }

        private async Task WhenTheBatchUploadIsLoaded(string batchId)
            => await _reader.LoadBatchUpload(new BatchUploadBlobName(batchId));

        private void GivenTheUploadedExcelFile(string batchId,
            string resourcePath)
        {
            GivenTheUploadedExcelFile(batchId, 
                GetType()
                    .Assembly
                    .GetManifestResourceStream($"CalculateFunding.Services.Publishing.UnitTests.Batches.Resources.{resourcePath}"));
        }
        
        private void GivenTheUploadedExcelFile(string batchId,
            Stream stream)
        {
            BatchUploadBlobName blobName = new BatchUploadBlobName(batchId);
            
            Mock<ICloudBlob> blob = new Mock<ICloudBlob>();

            _blobClient.Setup(_ => _.GetBlobReferenceFromServerAsync(blobName, "batchuploads"))
                .ReturnsAsync(blob.Object);

            _blobClient.Setup(_ => _.DownloadToStreamAsync(blob.Object))
                .ReturnsAsync(stream);
        }
        
        private string NewRandomString() => new RandomString();
    }
}