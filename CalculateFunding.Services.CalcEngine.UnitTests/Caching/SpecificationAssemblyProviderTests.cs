using System;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Calculator.Caching
{
    [TestClass]
    public class SpecificationAssemblyProviderTests
    {
        private Mock<IFileSystemCache> _fileSystemCache;
        private Mock<IBlobClient> _blobs;
        private Mock<ICloudBlob> _cloudBlob;

        private SpecificationAssemblyProvider _assemblyProvider;

        [TestInitialize]
        public void SetUp()
        {
            _fileSystemCache = new Mock<IFileSystemCache>();
            _blobs = new Mock<IBlobClient>();
            _cloudBlob = new Mock<ICloudBlob>();
            
            _assemblyProvider = new SpecificationAssemblyProvider(_fileSystemCache.Object,
                _blobs.Object,
                new CalculatorResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync()
                }, 
                Logger.None);
        }

        [TestMethod]
        public void GetAssembly_GuardsAgainstMissingSpecificationId()
        {
            Func<Task<Stream>> invocation = () => WhenTheAssemblyIsQueried(null, NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }
        [TestMethod]
        public void GetAssembly_GuardsAgainstMissingETag()
        {
            Func<Task<Stream>> invocation = () => WhenTheAssemblyIsQueried(NewRandomString(), null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("etag");
        }

        [TestMethod]
        public async Task GetAssembly_RetrievesFromFileSystemIfCached()
        {
            string specificationId = NewRandomString();
            string etag =  NewRandomETag();
            
            Stream expectedAssembly = new MemoryStream();
            
            GivenTheCachedAssembly(specificationId, etag, expectedAssembly);

            Stream actualAssembly = await WhenTheAssemblyIsQueried(specificationId, etag);

            actualAssembly
                .Should()
                .BeSameAs(expectedAssembly);
        }

        [TestMethod]
        public async Task GetAssembly_RetrievesFromBlobStorageIfNotInFileSystemCache()
        {
            string specificationId = NewRandomString();
            string etag =  NewRandomETag();
            
            Stream expectedAssembly = new MemoryStream();
            
            GivenTheAssemblyInBlobStorage(specificationId, expectedAssembly);
            AndTheBlobHasTheEtag(etag);

            Stream actualAssembly = await WhenTheAssemblyIsQueried(specificationId, etag);

            actualAssembly
                .Should()
                .BeSameAs(expectedAssembly);
            
            AndTheAssemblyWasCachedToTheFileSystem(specificationId, etag, expectedAssembly);
        }
        
        [TestMethod]
        public void GetAssembly_GuardsAgainstAssemblyFromBlobStorageHavingDifferentEtag()
        {
            string specificationId = NewRandomString();
            string etag =  NewRandomETag();
            
            Stream expectedAssembly = new MemoryStream();
            
            GivenTheAssemblyInBlobStorage(specificationId, expectedAssembly);
            AndTheBlobHasTheEtag(NewRandomString());
            
            Func<Task<Stream>> invocation = () => WhenTheAssemblyIsQueried(specificationId, etag);

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Invalid specification assembly etag.");
            
            AndTheAssemblyWasNotCachedToTheFileSystem(specificationId, etag, expectedAssembly);
        }

        [TestMethod]
        public void SetAssembly_GuardsAgainstMissingSpecificationId()
        {
            Func<Task> invocation = () => WhenTheAssemblyIsSet(null, new MemoryStream());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }
        
        [TestMethod]
        public void SetAssembly_GuardsAgainstMissingAssembly()
        {
            Func<Task> invocation = () => WhenTheAssemblyIsSet(NewRandomString(), null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("assembly");      
        }
        
        [TestMethod]
        public void SetAssembly_GuardsAgainstNoAssemblyInBlobStorage()
        {
            Func<Task> invocation = () => WhenTheAssemblyIsSet(NewRandomString(), new MemoryStream());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("assemblyBlob");      
        }

        [TestMethod]
        public async Task SetAssembly_AddsAssemblyToFileSystemCache()
        {
            string specificationId = NewRandomString();
            string etag = NewRandomString();
            Stream assembly = new MemoryStream();

            GivenTheAssemblyInBlobStorage(specificationId, assembly);
            AndTheBlobHasTheEtag(etag);

            await WhenTheAssemblyIsSet(specificationId, assembly);
            
            ThenTheAssemblyWasNotCachedToTheFileSystem(specificationId, etag, assembly);
            AndTheBlobPropertiesWereFetched();
        }

        private async Task WhenTheAssemblyIsSet(string specificationId,
            Stream assembly)
            => await _assemblyProvider.SetAssembly(specificationId, assembly);

        private void GivenTheCachedAssembly(string specificationId,
            string etag,
            Stream assembly)
        {
            string cacheKey = GetCacheKey(specificationId, etag);

            _fileSystemCache.Setup(_ => _.Exists(It.Is<SpecificationAssemblyFileSystemCacheKey>(key =>
                    key.Key == cacheKey)))
                .Returns(true);
            _fileSystemCache.Setup(_ => _.Get(It.Is<SpecificationAssemblyFileSystemCacheKey>(key =>
                    key.Key == cacheKey)))
                .Returns(assembly);
        }

        private static string GetCacheKey(string specificationId,
            string etag) =>
            $"SpecificationAssembly_{specificationId}_{etag.Replace("\"", "")}";

        private void GivenTheAssemblyInBlobStorage(string specificationId,
            Stream assembly)
        {
            string blobName = $"{specificationId}/implementation.dll";

            _blobs
                .Setup(_ => _.GetBlobReferenceFromServerAsync(blobName, null))
                .ReturnsAsync(_cloudBlob.Object);

            _blobs.Setup(_ => _.DownloadToStreamAsync(_cloudBlob.Object))
                .ReturnsAsync(assembly);
        }

        private void AndTheBlobPropertiesWereFetched()
            => _cloudBlob.Verify(_ => _.FetchAttributesAsync(), Times.Once);

        private void AndTheBlobHasTheEtag(string etag)
        {
            BlobProperties blobProperties = new BlobProperties();
            
            blobProperties.SetWithNonePublicSetter(_ => _.ETag, etag);
            
            _cloudBlob.Setup(_ => _.Properties)
                .Returns(blobProperties);
        }

        private void AndTheAssemblyWasCachedToTheFileSystem(string specificationId,
            string etag,
            Stream assembly)
            => _fileSystemCache.Verify(_ => _.Add(It.Is<SpecificationAssemblyFileSystemCacheKey>(key =>
                    key.Key == GetCacheKey(specificationId, etag)), assembly, default, true),
                Times.Once);

        private void ThenTheAssemblyWasNotCachedToTheFileSystem(string specificationId,
            string etag,
            Stream assembly)
            => AndTheAssemblyWasCachedToTheFileSystem(specificationId, etag, assembly);
        
        private void AndTheAssemblyWasNotCachedToTheFileSystem(string specificationId,
            string etag,
            Stream assembly)
            => _fileSystemCache.Verify(_ => _.Add(It.Is<SpecificationAssemblyFileSystemCacheKey>(key =>
                    key.Key == GetCacheKey(specificationId, etag)), assembly, default, true),
                Times.Never);
        
        private async Task<Stream> WhenTheAssemblyIsQueried(string specificationId,
            string etag)
            => await _assemblyProvider.GetAssembly(specificationId, etag);
        
        private string NewRandomString() => new RandomString();

        private string NewRandomETag() => $"\"{NewRandomString()}\"";

    }
}