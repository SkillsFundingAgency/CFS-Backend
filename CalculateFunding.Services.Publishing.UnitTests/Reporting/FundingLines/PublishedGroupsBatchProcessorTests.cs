using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{

    [TestClass]
    public class PublishedGroupsBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedGroupsCsvBatchProcessor(PublishedFunding.Object,              
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object);
        }

        [TestMethod]
        
        [DataRow(FundingLineCsvGeneratorJobType.PublishedGroups, true)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        public void SupportedJobTypes(FundingLineCsvGeneratorJobType jobType,
            bool expectedIsSupportedFlag)
        {
            BatchProcessor.IsForJobType(jobType)
                .Should()
                .Be(expectedIsSupportedFlag);
        }

        [TestMethod]
        public async Task ReturnsFalseIfNoResultsProcessed()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, fundingPeriodId, NewRandomString(), NewRandomString(), NewRandomString());

            processedResults
                .Should()
                .BeFalse();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.PublishedGroups)]
        public async Task TransformsPublishedGroupsForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();

            string expectedInterimFilePath = NewRandomString();

            IEnumerable<PublishedFunding> publishedFundingOne = new List<PublishedFunding>
            {
                NewPublishedFunding(_ => _.WithCurrent(
                    NewPublishedFundingVersion(ppv => ppv.WithFundingId("f-id")
                    .WithProviderFundings(new List<string>())))),
            };

            IEnumerable<PublishedFunding> publishedFundingWithProviderTwo = new[]
            {
                NewPublishedFunding(_ => _.WithCurrent(
                    NewPublishedFundingVersion(ppv => ppv.WithFundingId("f-id1")))),
                NewPublishedFunding(_ => _.WithCurrent(
                    NewPublishedFundingVersion(ppv => ppv.WithFundingId("f-id2")))),
                NewPublishedFunding(_ => _.WithCurrent(
                    NewPublishedFundingVersion(ppv => ppv.WithFundingId("f-id3"))))
            };

            ExpandoObject[] transformedRowsOne = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };
            ExpandoObject[] transformedRowsTwo = {
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
                new ExpandoObject(),
            };

            string expectedCsvOne = NewRandomString();
            string expectedCsvTwo = NewRandomString();

            string predicate = NewRandomString();
            string joinPredicate = NewRandomString();

            GivenTheCsvRowTransformation(publishedFundingOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedFundingWithProviderTwo, transformedRowsTwo, expectedCsvTwo, false);
            AndThePredicate(jobType, predicate);
            AndTheJoinPredicate(jobType, joinPredicate);

            PublishedFunding.Setup(_ => _.PublishedGroupBatchProcessing(specificationId,
                    It.IsAny<Func<List<PublishedFunding>, Task>>(),
                    100))
                .Callback<string, Func<List<PublishedFunding>, Task>, int>((spec,
                    batchProcessor, batchSize) =>
                {
                    batchProcessor(publishedFundingOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, fundingPeriodId, expectedInterimFilePath, fundingLineCode, fundingStreamId);

            processedResults
                .Should()
                .BeTrue();

        }
    }
}
