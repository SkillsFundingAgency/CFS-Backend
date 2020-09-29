using CacheCow.Server;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Caching.Http;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CalculateFunding.Services.Publishing.UnitTests.Caching.Http
{
    [TestClass]
    public class PublishedProviderFundingStructureTimedEtagExtractorTests
    {
        PublishedProviderFundingStructureTimedEtagExtractor _extractor;

        [TestInitialize]
        public void Setup()
        {
            _extractor = new PublishedProviderFundingStructureTimedEtagExtractor();
        }

        [TestMethod]
        public void ExtractsHeaderValueOfViewModelFromFundingStructurePublishedProviderVersionProperty()
        {
            PublishedProviderFundingStructure fundingStructure = new PublishedProviderFundingStructure() { PublishedProviderVersion = new RandomNumberBetween(1, int.MaxValue)};

            TimedEntityTagHeaderValue headerValue = WhenTheHeaderValueIsExtracted(fundingStructure);

            headerValue
                ?.ETag
                ?.Tag
                .Should()
                .Be($"\"{fundingStructure.PublishedProviderVersion.ToString()}\"");
        }

        [TestMethod]
        public void GuardsAgainstIncorrectArgumentType()
        {
            Func<TimedEntityTagHeaderValue> invocation = () => WhenTheHeaderValueIsExtracted(new object());

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("viewModel");
        }

        private TimedEntityTagHeaderValue WhenTheHeaderValueIsExtracted(object viewModel)
            => _extractor.Extract(viewModel);
    }
}
