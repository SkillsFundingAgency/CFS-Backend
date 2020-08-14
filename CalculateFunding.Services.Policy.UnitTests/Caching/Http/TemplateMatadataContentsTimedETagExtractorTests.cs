using System;
using CacheCow.Server;
using CalculateFunding.Common.TemplateMetadata.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Policy.Caching.Http
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
            TemplateMetadataContents metadataContents = NewTemplateMetadataContents();

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
        
        private TemplateMetadataContents NewTemplateMetadataContents() => new TemplateMetadataContentsBuilder()
            .Build();
    }
}