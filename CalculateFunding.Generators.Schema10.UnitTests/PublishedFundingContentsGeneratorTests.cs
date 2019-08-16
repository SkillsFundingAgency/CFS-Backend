using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Generators.Schema10.UnitTests
{
    [TestClass]
    public class PublishedFundingContentsGeneratorTests
    {
        [TestMethod]
        public void ThrowsExceptionWhenPublishedFundingVersionNotSupplied()
        {
            Func<string> invocation = () => WhenThePublishedFundingVersionIsTransformed(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("publishedFundingVersion");
        }

        [TestMethod]
        public void TransformsSuppliedPublishedFundingVersionIntoSchema10Json()
        {
            PublishedFundingVersion publishedFundingVersion =
                ReadResourceFileContents("CalculateFunding.Generators.Schema10.UnitTests.Resources.examplePublishedFundingVersion_1.json")
                    .AsPoco<PublishedFundingVersion>();
            
            string expectedJson = ReadResourceFileContents("CalculateFunding.Generators.Schema10.UnitTests.Resources.expectedPublishedFundingSchema1Contents_1.json")
                .Prettify();

            string actualJson = WhenThePublishedFundingVersionIsTransformed(publishedFundingVersion);

            actualJson
                .Prettify()
                .Should()
                .Be(expectedJson);
        }

        private string WhenThePublishedFundingVersionIsTransformed(PublishedFundingVersion publishedFundingVersion)
        {
            return new PublishedFundingContentsGenerator()
                .GenerateContents(publishedFundingVersion);
        }

        private string ReadResourceFileContents(string resourcePath)
        {
            return typeof(PublishedFundingContentsGeneratorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourcePath);
        }
    }
}