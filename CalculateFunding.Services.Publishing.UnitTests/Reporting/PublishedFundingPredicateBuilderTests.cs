
using System;
using CalculateFunding.Services.Publishing.Reporting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{

    [TestClass]
    public class PublishedFundingPredicateBuilderTests
    {
        private PublishedFundingPredicateBuilder _predicateBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _predicateBuilder = new PublishedFundingPredicateBuilder();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, "1 = 1")]
        public void ReturnsPredicatesAppropriateToJobTypes(FundingLineCsvGeneratorJobType jobType,
            string expectedPredicate)
        {
            _predicateBuilder.BuildPredicate(jobType)
                .Should()
                .Be(expectedPredicate);
        }

        [TestMethod]
        public void ThrowsExceptionWhenJobTypeNotConfigured()
        {
            Func<string> invocation = () => 
                _predicateBuilder.BuildPredicate(FundingLineCsvGeneratorJobType.Undefined);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>();
        }
    }
}