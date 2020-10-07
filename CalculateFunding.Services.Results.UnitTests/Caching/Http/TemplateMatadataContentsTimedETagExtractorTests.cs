using System;
using CacheCow.Server;
using CalculateFunding.Models.Result;
using CalculateFunding.Services.Results.Caching.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests.Caching.Http
{
    [TestClass]
    public class TemplateMatadataContentsTimedETagExtractorTests
    {
        private TemplateMatadataContentsTimedETagExtractor _extractor;

        [TestInitialize]
        public void SetUp()
        {
            _extractor = new TemplateMatadataContentsTimedETagExtractor();
        }

        [TestMethod]
        public void ExtractsHeaderValueOffViewModelLastModifiedProperty()
        {
            FundingStructure metadataContents = NewFundingStructure();

            TimedEntityTagHeaderValue headerValue = WhenTheHeaderValueIsExtracted(metadataContents);

            headerValue
                ?.ETag
                ?.Tag
                .Should()
                .Be($"\"{metadataContents.LastModified.ToETagString()}\"");
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
        
        private FundingStructure NewFundingStructure() => new FundingStructureBuilder()
            .Build();
    }
}