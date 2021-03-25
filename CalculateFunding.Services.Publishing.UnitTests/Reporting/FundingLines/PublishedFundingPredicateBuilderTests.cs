using System;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
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
        [DataRow(FundingLineCsvGeneratorJobType.Released, "NOT IS_NULL(c.content.released)")]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, "1 = 1")]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, "1 = 1")]
        [DataRow(FundingLineCsvGeneratorJobType.History, "1 = 1")]
        public void ReturnsPredicatesAppropriateToJobTypes(FundingLineCsvGeneratorJobType jobType,
            string expectedPredicate)
        {
            _predicateBuilder.BuildPredicate(jobType)
                .Should()
                .Be(expectedPredicate);
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, "WHERE fundingLine.name = @fundingLineCode")]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, "WHERE fundingLine.name = @fundingLineCode")]
        public void ReturnsJoinPredicatesAppropriateToJobTypes(FundingLineCsvGeneratorJobType jobType,
            string expectedPredicate)
        {
            _predicateBuilder.BuildJoinPredicate(jobType)
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