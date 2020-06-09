using System;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingContentsGeneratorResolverTests
    {
        private PublishedFundingContentsGeneratorResolver _resolver;

        [TestInitialize]
        public void SetUp()
        {
            _resolver = new PublishedFundingContentsGeneratorResolver();
        }

        [TestMethod]
        public void ThrowsExceptionIfSchemaVersionNotSupported()
        {
            Func<IPublishedFundingContentsGenerator> invocation = () => WhenTheGeneratorIsResolved(NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("schemaVersion");
        }

        [TestMethod]
        public void RegistersGeneratorsByTheSuppliedSchemaVersions()
        {
            string versionOne = NewRandomString();
            string versionTwo = NewRandomString();
            string versionThree = NewRandomString();
            IPublishedFundingContentsGenerator generatorOne = NewGenerator();
            IPublishedFundingContentsGenerator generatorTwo = NewGenerator();
            IPublishedFundingContentsGenerator generatorThree = NewGenerator();
            
            GivenTheGenerators((versionOne, generatorOne),
                (versionTwo, generatorTwo),
                (versionThree, generatorThree));

            IPublishedFundingContentsGenerator resolvedGenerator = WhenTheGeneratorIsResolved(versionTwo);

            resolvedGenerator
                .Should()
                .BeSameAs(generatorTwo);
        }

        private void GivenTheGenerators(params (string version, IPublishedFundingContentsGenerator generator)[] generators)
        {
            foreach ((string version, IPublishedFundingContentsGenerator generator) generator in generators)
            {
                _resolver.Register(generator.version, 
                    generator.generator);
            }
        }

        private IPublishedFundingContentsGenerator WhenTheGeneratorIsResolved(string version) => _resolver.GetService(version);
        
        private IPublishedFundingContentsGenerator NewGenerator() => new Mock<IPublishedFundingContentsGenerator>().Object;
        
        private string NewRandomString() => new RandomString();
    }
}