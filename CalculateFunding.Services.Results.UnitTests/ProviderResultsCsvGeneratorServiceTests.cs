using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class ProviderResultsCsvGeneratorServiceTests
    {
        private ProviderResultsCsvGeneratorService _service;
        private ICsvUtils _csvUtils;
        private IBlobClient _blobClient;
        private ICloudBlob _cloudBlob; 
        private IProverResultsToCsvRowsTransformation _transformation;
        private ICalculationResultsRepository _calculationResultsRepository;
        private IFileSystemAccess _fileSystemAccess;
        private IFileSystemCacheSettings _fileSystemCacheSettings;
        private string _rootPath;
        private BlobProperties _blobProperties;

        private Message _message;
        
        [TestInitialize]
        public void SetUp()
        {
            _calculationResultsRepository = Substitute.For<ICalculationResultsRepository>();
            _blobClient = Substitute.For<IBlobClient>();
            _csvUtils = Substitute.For<ICsvUtils>();
            _transformation = Substitute.For<IProverResultsToCsvRowsTransformation>();
            _cloudBlob = Substitute.For<ICloudBlob>();
            _fileSystemAccess = Substitute.For<IFileSystemAccess>();
            _fileSystemCacheSettings = Substitute.For<IFileSystemCacheSettings>();
            
            _service = new ProviderResultsCsvGeneratorService(Substitute.For<ILogger>(),
                _blobClient,
                _calculationResultsRepository,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    ResultsRepository =  Policy.NoOpAsync()
                }, 
                _csvUtils,
                _transformation,
                _fileSystemAccess,
                _fileSystemCacheSettings);
            
            _message = new Message();
            _rootPath = NewRandomString();

            _fileSystemCacheSettings.Path
                .Returns(_rootPath);

            _fileSystemAccess
                .Append(Arg.Any<string>(), Arg.Any<Stream>())
                .Returns(Task.CompletedTask);

            _blobProperties = new BlobProperties();

            _cloudBlob
                .Properties
                .Returns(_blobProperties);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoSpecificationIdInMessageProperties()
        {
            Func<Task> invocation = WhenTheCsvIsGenerated;

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage("Specification id missing");
        }

        [TestMethod]
        public async Task TransformsProviderResultsForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string specificationName = NewRandomString();
            string expectedInterimFilePath = Path.Combine(_rootPath, $"calculation-results-{specificationId}.csv");
            
            List<ProviderResult> providerResultsOne = new List<ProviderResult>();
            List<ProviderResult> providerResultsTwo = new List<ProviderResult>();
            
            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            ExpandoObject[] transformedRowsTwo = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            
            string expectedCsvOne = NewRandomString();
            string expectedCsvTwo = NewRandomString();
            
            StreamWriter streamWriterOne = new StreamWriter(new MemoryStream(expectedCsvOne.AsUTF8Bytes()));
            StreamWriter streamWriterTwo = new StreamWriter(new MemoryStream(expectedCsvTwo.AsUTF8Bytes()));
            
            MemoryStream incrementalFileStream = new MemoryStream();

            GivenTheCsvRowTransformation(providerResultsOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(providerResultsTwo, transformedRowsTwo, expectedCsvTwo,  false);
            AndTheMessageProperties(("specification-id", specificationId));
            AndTheMessageProperties(("specification-name", specificationName));
            AndTheCloudBlobForSpecificationId(specificationId);
            AndTheFileStream(expectedInterimFilePath, incrementalFileStream);
            AndTheFileExists(expectedInterimFilePath);
            
            _calculationResultsRepository
                .When(_ =>
                    _.ProviderResultsBatchProcessing(Arg.Is(specificationId), 
                        Arg.Any<Func<List<ProviderResult>, Task>>(), 
                        Arg.Is(100)))
                .Do(info =>
                {
                    Func<List<ProviderResult>,Task> batchProcessingCallBack = info.ArgAt<Func<List<ProviderResult>, Task>>(1);
                    
                    batchProcessingCallBack(providerResultsOne)
                        .GetAwaiter()
                        .GetResult();
                    batchProcessingCallBack(providerResultsTwo)
                        .GetAwaiter()
                        .GetResult();
                });

            await WhenTheCsvIsGenerated();
            
            _fileSystemAccess
                .Received(1)
                .Delete(expectedInterimFilePath);

            await _fileSystemAccess
                .Append(expectedInterimFilePath, streamWriterOne.BaseStream);
            
            await _fileSystemAccess
                .Append(expectedInterimFilePath, streamWriterTwo.BaseStream);
            
            await _blobClient
                .Received(1)
                .UploadAsync(_cloudBlob, incrementalFileStream);

            await _blobClient
                .Received(1)
                .AddMetadataAsync(
                _cloudBlob, 
                Arg.Is<IDictionary<string, string>>(_=> 
                    _["specification-id"] == specificationId &&
                    _["specification-name"] == specificationName &&
                    _["file-name"].StartsWith($"Calculation Results {specificationName} {DateTimeOffset.UtcNow:yyyy-MM-dd}") &&
                    _["job-type"] == "CalcResult")
                );
        }

        private void AndTheCloudBlobForSpecificationId(string specificationId)
        {
            _blobClient
                .GetBlockBlobReference($"calculation-results-{specificationId}.csv")
                .Returns(_cloudBlob);
        }

        private void AndTheFileStream(string path, Stream stream)
        {
            _fileSystemAccess
                .OpenRead(path)
                .Returns(stream);
        }

        private void AndTheFileExists(string path)
        {
            _fileSystemAccess
                .Exists(path)
                .Returns(true);
        }

        private void AndTheCsvRowTransformation(List<ProviderResult> providerResults, ExpandoObject[] transformedRows, string csv, bool outputHeaders)
        {
            GivenTheCsvRowTransformation(providerResults, transformedRows, csv, outputHeaders);
        }

        private void GivenTheCsvRowTransformation(List<ProviderResult> providerResult, ExpandoObject[] transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .TransformProviderResultsIntoCsvRows(Arg.Is(providerResult))
                .Returns(transformedRows);
            
            _csvUtils.AsCsv(Arg.Is<IEnumerable<dynamic>>(_ => _.SequenceEqual(transformedRows)), Arg.Is(outputHeaders))
                .Returns(csv);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Run(_message);
        }

        private void AndTheMessageProperties(params (string, string)[] properties)
        {
            GivenTheMessageProperties(properties);            
        }

        private void GivenTheMessageProperties(params (string,string)[] properties)
        {
            _message.AddUserProperties(properties);
        }
    }
}