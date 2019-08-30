using System;
using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Generators.Schema10.UnitTests
{
    [TestClass]
    public class PublishedFundingContentsGeneratorTests
    {
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
        public void TransformsSuppliedPublishedFundingVersionIntoSchema10Json_2()
        {
            PublishedFundingVersion publishedFundingVersion =
                ReadResourceFileContents("CalculateFunding.Generators.Schema10.UnitTests.Resources.examplePublishedFundingVersion_2.json")
                    .AsPoco<PublishedFundingVersion>();

            string expectedJson = ReadResourceFileContents("CalculateFunding.Generators.Schema10.UnitTests.Resources.expectedPublishedFundingSchema1Contents_2.json")
                .Prettify();

            TemplateMetadataContents templateMetadataContents = ReadTemplateContents(ReadResourceFileContents(
                "CalculateFunding.Generators.Schema10.UnitTests.Resources.exampleTemplate1.json"));

            string actualJson = WhenThePublishedFundingVersionIsTransformed(publishedFundingVersion, templateMetadataContents);

            actualJson = actualJson
                .Prettify();
            
            actualJson
                .Should()
                .Be(expectedJson);
        }

        private string WhenThePublishedFundingVersionIsTransformed(PublishedFundingVersion publishedFundingVersion, 
            TemplateMetadataContents templateMetadataContents)
        {
            return new PublishedFundingContentsGenerator()
                .GenerateContents(publishedFundingVersion, templateMetadataContents);
        }

        private string ReadResourceFileContents(string resourcePath)
        {
            return typeof(PublishedFundingContentsGeneratorTests)
                .Assembly    
                .GetEmbeddedResourceFileContents(resourcePath);
        }

        private TemplateMetadataContents ReadTemplateContents(string contents)
        {
            return new TemplateMetadataGenerator(Substitute.For<ILogger>())
                .GetMetadata(contents);
        }
    }
}