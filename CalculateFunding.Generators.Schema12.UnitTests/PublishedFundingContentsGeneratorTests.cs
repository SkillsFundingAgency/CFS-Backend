using System;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema12;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;

namespace CalculateFunding.Generators.Schema12.UnitTests
{
    [TestClass]
    public class PublishedFundingContentsGeneratorTests : FundingSchema12TestBase
    {
        private PublishedFundingContentsGenerator _generator;

        [TestInitialize]
        public void SetUp()
        {
            _generator = new PublishedFundingContentsGenerator();
        }

        [TestMethod]
        public void ThrowsExceptionWhenPublishedFundingVersionNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedFundingVersionIsTransformed(null, new TemplateMetadataContents());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("publishedFundingVersion");
        }

        [TestMethod]
        public void ThrowsExceptionWhenTemplateMetadataContentsNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedFundingVersionIsTransformed(new PublishedFundingVersion(), null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("templateMetadataContents");
        }

        [TestMethod]
        public void GeneratesJsonConformingToThe12Schema()
        {
            TemplateMetadataContents templateMetadataContents = GetTemplateMetaDataContents("example-funding-template1.2.json");
            PublishedFundingVersion publishedFundingVersion = GetPublishedFundingVersion("example-published-funding-version.json");

            string funding = WhenThePublishedFundingVersionIsTransformed(publishedFundingVersion, templateMetadataContents);

            ThenTheJsonValidatesAgainstThe1_1FundingSchema(funding);
        }

        private PublishedFundingVersion GetPublishedFundingVersion(string resourceName)
            => GetEmbeddedFileContents(resourceName)
                .AsPoco<PublishedFundingVersion>();

        private TemplateMetadataContents GetTemplateMetaDataContents(string resourceName)
            => new TemplateMetadataGenerator(Logger.None)
                .GetMetadata(GetEmbeddedFileContents(resourceName));

        private string WhenThePublishedFundingVersionIsTransformed(PublishedFundingVersion publishedFundingVersion,
            TemplateMetadataContents templateMetadataContents)
            => _generator.GenerateContents(publishedFundingVersion,
                templateMetadataContents);
    }
}