using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
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

namespace CalculateFunding.Services.Results
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

        private Message _message;
        
        [TestInitialize]
        public void SetUp()
        {
            _calculationResultsRepository = Substitute.For<ICalculationResultsRepository>();
            _blobClient = Substitute.For<IBlobClient>();
            _csvUtils = Substitute.For<ICsvUtils>();
            _transformation = Substitute.For<IProverResultsToCsvRowsTransformation>();
            _cloudBlob = Substitute.For<ICloudBlob>();
            
            _service = new ProviderResultsCsvGeneratorService(Substitute.For<ILogger>(),
                _blobClient,
                _calculationResultsRepository,
                new ResiliencePolicies
                {
                    BlobClient = Policy.NoOpAsync(),
                    ResultsRepository =  Policy.NoOpAsync()
                }, 
                _csvUtils,
                _transformation);
            
            _message = new Message();
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
            string specificationId = new RandomString();
            
            List<ProviderResult> providerResults = new List<ProviderResult>();
            
            dynamic[] transformedRows = {
                new object(),
                new object(),
                new object(),
                new object(),
            };

            _transformation
                .TransformProviderResultsIntoCsvRows(providerResults)
                .Returns(transformedRows);
            
            _calculationResultsRepository
                .When(_ => 
                    _.ProviderResultsBatchProcessing(Arg.Is(specificationId), Arg.Any<Func<List<ProviderResult>, Task>>()))
                .Do(info => info.ArgAt<Func<List<ProviderResult>, Task>>(1)(providerResults));

            _blobClient
                .GetBlockBlobReference($"calculation-results-{specificationId}")
                .Returns(_cloudBlob);
            
            string expectedCsv = new RandomString();

            _csvUtils.AsCsv(Arg.Is<List<dynamic>>(_ => _.SequenceEqual(transformedRows)))
                .Returns(expectedCsv);
            
            GivenTheMessageProperties(("specification-id", specificationId));

            await WhenTheCsvIsGenerated();

            await _blobClient
                .Received(1)
                .UploadAsync(_cloudBlob, expectedCsv);
        }

        private async Task WhenTheCsvIsGenerated()
        {
            await _service.Run(_message);
        } 

        private void GivenTheMessageProperties(params (string,string)[] properties)
        {
            foreach ((string, string) property in properties)
            {
                _message.UserProperties.Add(property.Item1, property.Item2);
            }   
        }
    }
}