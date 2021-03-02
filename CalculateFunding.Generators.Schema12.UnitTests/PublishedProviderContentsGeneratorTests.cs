using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
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
    public class PublishedProviderContentsGeneratorTests : FundingSchema12TestBase
    {
        private PublishedProviderContentsGenerator _generator;

        [TestInitialize]
        public void SetUp()
        {
            _generator = new PublishedProviderContentsGenerator();
        }

        [TestMethod]
        public void ThrowsExceptionWhenPublishedFundingVersionNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedProviderVersionIsTransformed(null, new TemplateMetadataContents(), new TemplateMapping());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("publishedProviderVersion");
        }

        [TestMethod]
        public void ThrowsExceptionWhenTemplateMetadataContentsNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedProviderVersionIsTransformed(new PublishedProviderVersion(),
                null,
                new TemplateMapping());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("templateMetadataContents");
        }

        [TestMethod]
        public void ThrowsExceptionWhenTemplateMappingNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedProviderVersionIsTransformed(new PublishedProviderVersion(),
                new TemplateMetadataContents(),
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("templateMapping");
        }

        [TestMethod]
        public void GeneratesJsonConformingToThe12Schema()
        {
            TemplateMetadataContents templateMetadataContents = GetTemplateMetaDataContents("example-funding-template1.2.json");
            PublishedProviderVersion publishedProviderVersion = GetPublishedProviderVersion("example-published-provider-version.json");
            TemplateMapping templateMapping = new TemplateMapping
            {
                TemplateMappingItems = publishedProviderVersion.Calculations.Select(_ => new TemplateMappingItem
                {
                    CalculationId = _.TemplateCalculationId.ToString(),
                    TemplateId = _.TemplateCalculationId
                })
            };

            string publishedProvider = WhenThePublishedProviderVersionIsTransformed(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Prettify();

            string publishedProviderExample = GetEmbeddedFileContents("published_provider_example_1.2.json");

            publishedProvider
                .Should()
                .Be(publishedProviderExample);
        }

        private PublishedProviderVersion GetPublishedProviderVersion(string resourceName)
            => GetEmbeddedFileContents(resourceName)
                .AsPoco<PublishedProviderVersion>();

        private TemplateMetadataContents GetTemplateMetaDataContents(string resourceName)
            => new TemplateMetadataGenerator(Logger.None)
                .GetMetadata(GetEmbeddedFileContents(resourceName));

        private string WhenThePublishedProviderVersionIsTransformed(PublishedProviderVersion publishedProviderVersion,
            TemplateMetadataContents templateMetadataContents,
            TemplateMapping templateMapping)
            => _generator.GenerateContents(publishedProviderVersion,
                templateMetadataContents,
                templateMapping);
    }
}