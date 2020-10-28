using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Services.Results.SearchIndex;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.SearchIndex
{
    [TestClass]
    public class ProviderCalculationResultsIndexDataReaderTests
    {
        private ICalculationResultsRepository _repository;
        private ProviderCalculationResultsIndexDataReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _repository = Substitute.For<ICalculationResultsRepository>();
            _reader = new ProviderCalculationResultsIndexDataReader(_repository);
        }

        [TestMethod]
        public async Task ShouldGetProviderResultById()
        {
            string providerResultId = new RandomString();
            string partitionKey = new RandomString();
            ProviderResultDataKey providerResultDataKey = new ProviderResultDataKey(providerResultId, partitionKey);

            ProviderResult expectedProviderResult = new ProviderResult() { Id = providerResultId };
            _repository.GetProviderResultById(Arg.Is(providerResultId), Arg.Is(partitionKey))
                .Returns(expectedProviderResult);

            ProviderResult providerResult = await _reader.GetData(providerResultDataKey);

            providerResult
                .Should()
                .Be(expectedProviderResult);

            await _repository
                .Received(1)
                .GetProviderResultById(Arg.Is(providerResultId), Arg.Is(partitionKey));

        }
    }
}
