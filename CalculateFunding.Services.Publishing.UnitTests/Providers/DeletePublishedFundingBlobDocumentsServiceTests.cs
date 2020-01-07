using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeletePublishedFundingBlobDocumentsServiceTests
    {
        private IBlobClient _blobClient;

        private DeletePublishedFundingBlobDocumentsService _service;
        
        private string _containerName;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _matchingFileStart;

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = Substitute.For<IBlobClient>();

            _containerName = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
            
            _matchingFileStart = $"{_fundingStreamId}-{_fundingPeriodId}";
            
            _service = new DeletePublishedFundingBlobDocumentsService(new ResiliencePolicies
            {
                BlobClient = Policy.NoOpAsync()
            }, 
                _blobClient,
                Substitute.For<ILogger>());
        }

        [TestMethod]
        public async Task RemovesAllBlobsForFundingStreamAndPeriodInContainerWithSuppliedName()
        {
            string blobOne = NewRandomMatchingUri();
            string blobTwo = NewRandomMatchingUri();
            string blobThree = NewRandomString();
            string blobFour = NewRandomMatchingUri();
            
            GivenTheBlobsInTheContainer(NewBlobItem(blobOne),
                NewBlobItem(blobTwo),
                NewBlobItem(blobThree),
                NewBlobItem(blobFour));

            ICloudBlob blobRefOne = AndTheCloudBlob(blobOne); 
            ICloudBlob blobRefTwo = AndTheCloudBlob(blobTwo); 
            ICloudBlob blobRefThree = AndTheCloudBlob(blobThree); 
            ICloudBlob blobRefFour = AndTheCloudBlob(blobFour); 

            await WhenTheBlobsAreDeletedForTheFundingStreamAndPeriod();

            await ThenTheBlobsWereDeleted(blobRefOne, blobRefTwo, blobRefFour);
            await AndTheBlobsWereNotDeleted(blobRefThree);
        }

        private async Task ThenTheBlobsWereDeleted(params ICloudBlob[] blobRefs)
        {
            foreach (ICloudBlob cloudBlob in blobRefs)
            {
                await cloudBlob
                    .Received(1)
                    .DeleteAsync();
            }
        }
        
        private async Task AndTheBlobsWereNotDeleted(params ICloudBlob[] blobRefs)
        {
            foreach (ICloudBlob cloudBlob in blobRefs)
            {
                await cloudBlob
                    .Received(0)
                    .DeleteAsync();
            }
        }

        private void GivenTheBlobsInTheContainer(params IListBlobItem[] blobs)
        {
            _blobClient
                .BatchProcessBlobs(Arg.Any<Func<IEnumerable<IListBlobItem>, Task>>(),
                    Arg.Is(_containerName))
                .Returns(Task.CompletedTask)
                .AndDoes(_ => _.Arg<Func<IEnumerable<IListBlobItem>, Task>>()(blobs));
        }

        private ICloudBlob AndTheCloudBlob(string blobName)
        {
            ICloudBlob cloudBlob = Substitute.For<ICloudBlob>();

            _blobClient.GetBlobReferenceFromServerAsync(blobName, _containerName)
                .Returns(cloudBlob);
            
            return cloudBlob;
        }

        private async Task WhenTheBlobsAreDeletedForTheFundingStreamAndPeriod()
        {
            await _service.DeletePublishedFundingBlobDocuments(_fundingStreamId, _fundingPeriodId, _containerName);
        }

        private IListBlobItem NewBlobItem(string uri)
        {
            IListBlobItem blobItem = Substitute.For<IListBlobItem>();

            blobItem
                .Uri
                .Returns(new Uri(new Uri(@"http://baseUri"), new Uri(uri, UriKind.Relative)));

            return blobItem;
        }

        private string NewRandomMatchingUri()
        {
            return $"{_matchingFileStart}{NewRandomString()}";
        }
        
        private string NewRandomString() => new RandomString();
    }
}