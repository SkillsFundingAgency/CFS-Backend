using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.SearchIndex;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.SearchIndex
{
    [TestClass]
    public class ProviderCalculationResultsIndexProcessorTests
    {
        private ILogger _logger;
        private ISearchIndexDataReader<string, ProviderResult> _reader;
        private ISearchIndexTrasformer<ProviderResult, ProviderCalculationResultsIndex> _transformer;
        private ISearchRepository<ProviderCalculationResultsIndex> _searchRepository;
        private ISearchIndexWriterSettings _settings;
        private ProviderCalculationResultsIndexProcessor _processor;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger>();
            _reader = Substitute.For<ISearchIndexDataReader<string, ProviderResult>>();
            _transformer = Substitute.For<ISearchIndexTrasformer<ProviderResult, ProviderCalculationResultsIndex>>();
            _searchRepository = Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
            _settings = Substitute.For<ISearchIndexWriterSettings>();
            _settings.ProviderCalculationResultsIndexWriterDegreeOfParallelism.Returns(45);
            _processor = new ProviderCalculationResultsIndexProcessor(_logger, _reader, _transformer, _searchRepository, _settings);
        }

        [TestMethod]
        public async Task ShouldWriteSearchIndexForAllProviderResults_WhenProcessTheMessage_GivenProviderResultsForSepecificationAndProviderIds()
        {
            // Arrange
            string specificationId = new RandomString();
            string specificationName = new RandomString();
            string providerId1 = new RandomString();
            string providerId2 = new RandomString();

            string providerResultId1 = CreateProviderResultId(specificationId, providerId1);
            string providerResultId2 = CreateProviderResultId(specificationId, providerId2);

            ProviderResult providerResult1 = new ProviderResult() { Id = providerResultId1 };
            ProviderResult providerResult2 = new ProviderResult() { Id = providerResultId2 };

            ProviderCalculationResultsIndex index1 = new ProviderCalculationResultsIndex() { SpecificationId = specificationId, ProviderId = providerId1 };
            ProviderCalculationResultsIndex index2 = new ProviderCalculationResultsIndex() { SpecificationId = specificationId, ProviderId = providerId2 };

            _reader.GetData(Arg.Is(providerResultId1)).Returns(providerResult1);
            _reader.GetData(Arg.Is(providerResultId2)).Returns(providerResult2);

            _transformer.Transform(Arg.Is<ProviderResult>(_ => _.Id == providerResultId1), Arg.Is<ISearchIndexProcessorContext>(_ => _.GetType() == typeof(ProviderCalculationResultsIndexProcessorContext)))
                .Returns(index1);
            _transformer.Transform(Arg.Is<ProviderResult>(_ => _.Id == providerResultId2), Arg.Is<ISearchIndexProcessorContext>(_ => _.GetType() == typeof(ProviderCalculationResultsIndexProcessorContext)))
                .Returns(index2);

            _searchRepository.Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(_ => _.Any(x => x.ProviderId == providerId1 || x.ProviderId == providerId2)))
                .Returns(Enumerable.Empty<IndexError>());

            Message message = new Message();
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("specification-name", specificationName);
            message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new[] { providerId1, providerId2 }));

            // Act
            await _processor.Process(message);

            // Assert
            await _searchRepository
                 .Received(2)
                 .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(_ => _.Any(x => x.ProviderId == providerId1 || x.ProviderId == providerId2)));

            await _reader
                .Received(2)
                .GetData(Arg.Is<string>(_ => _ == providerResultId1 || _ == providerResultId2));

            await _transformer
                .Received(2)
                .Transform(Arg.Is<ProviderResult>(_ => _.Id == providerResultId1 || _.Id == providerResultId2),
                Arg.Is<ISearchIndexProcessorContext>(_ => _.GetType() == typeof(ProviderCalculationResultsIndexProcessorContext)));
        }

        [TestMethod]
        public async Task ShouldPartiallySuccessful_WhenProcessTheMessage_GivenOneOfTheIndexProcessingRaiseAnException()
        {
            // Arrange
            string specificationId = new RandomString();
            string specificationName = new RandomString();
            string providerId1 = new RandomString();
            string providerId2 = new RandomString();

            string providerResultId1 = CreateProviderResultId(specificationId, providerId1);
            string providerResultId2 = CreateProviderResultId(specificationId, providerId2);

            ProviderResult providerResult1 = new ProviderResult() { Id = providerResultId1 };
            ProviderResult providerResult2 = new ProviderResult() { Id = providerResultId2 };

            ProviderCalculationResultsIndex index1 = new ProviderCalculationResultsIndex() { SpecificationId = specificationId, ProviderId = providerId1 };
            ProviderCalculationResultsIndex index2 = new ProviderCalculationResultsIndex() { SpecificationId = specificationId, ProviderId = providerId2 };

            _reader.GetData(Arg.Is(providerResultId1)).Returns(Task.FromException<ProviderResult>(new Exception("Reader Failed")));
            _reader.GetData(Arg.Is(providerResultId2)).Returns(providerResult2);

            _transformer.Transform(Arg.Is<ProviderResult>(_ => _.Id == providerResultId2), Arg.Is<ISearchIndexProcessorContext>(_ => _.GetType() == typeof(ProviderCalculationResultsIndexProcessorContext)))
                .Returns(index2);

            _searchRepository.Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(_ => _.Any(x => x.ProviderId == providerId1 || x.ProviderId == providerId2)))
                .Returns(Enumerable.Empty<IndexError>());

            Message message = new Message();
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("specification-name", specificationName);
            message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new[] { providerId1, providerId2 }));

            // Act
            Func<Task> test = async() => await _processor.Process(message);

            // Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Error occurred while processing the ProviderCalculationResultsIndex. Reader Failed");

            await _searchRepository
                 .Received(1)
                 .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(_ => _.Any(x => x.ProviderId == providerId1 || x.ProviderId == providerId2)));

            await _reader
                .Received(2)
                .GetData(Arg.Is<string>(_ => _ == providerResultId1 || _ == providerResultId2));

            await _transformer
                .Received(1)
                .Transform(Arg.Is<ProviderResult>(_ => _.Id == providerResultId1 || _.Id == providerResultId2),
                Arg.Is<ISearchIndexProcessorContext>(_ => _.GetType() == typeof(ProviderCalculationResultsIndexProcessorContext)));
        }

        private string CreateProviderResultId(string specificationId, string providerId)
        {
            byte[] providerResultIdBytes = Encoding.UTF8.GetBytes($"{providerId}-{specificationId}");
            return Convert.ToBase64String(providerResultIdBytes);
        }
    }
}
