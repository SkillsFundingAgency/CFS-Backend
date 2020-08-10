using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedFundingVersionOrganisationGroupCsvBatchProcessor(new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object,
                PublishedFunding.Object);
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, false)]
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

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            processedResults
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task TransformsPublishedFundingForSpecificationInBatchesAndCreatesCsvWithResults()
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();

            string expectedInterimFilePath = NewRandomString();

            IEnumerable<PublishedFundingVersion> publishedFundingVersionsOne = new[]
            {
                NewPublishedFundingVersion()
            };
            IEnumerable<PublishedFundingVersion> publishedFundingVersionsTwo = new[]
            {
                NewPublishedFundingVersion(), NewPublishedFundingVersion(), NewPublishedFundingVersion()
            };

            ExpandoObject[] transformedRowsOne =
            {
                new ExpandoObject(), new ExpandoObject(), new ExpandoObject(), new ExpandoObject()
            };
            ExpandoObject[] transformedRowsTwo =
            {
                new ExpandoObject(), new ExpandoObject(), new ExpandoObject(), new ExpandoObject()
            };

            string expectedCsvOne = NewRandomString();
            string expectedCsvTwo = NewRandomString();

            GivenTheCsvRowTransformation(publishedFundingVersionsOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedFundingVersionsTwo, transformedRowsTwo, expectedCsvTwo, false);

            PublishedFunding.Setup(_ => _.PublishedFundingVersionBatchProcessing(specificationId,
                    fundingStreamId,
                    fundingPeriodId,
                    It.IsAny<Func<List<PublishedFundingVersion>, Task>>(),
                    100))
                .Callback<string, string, string, Func<List<PublishedFundingVersion>, Task>, int>((spec,
                    stream,
                    period,
                    batchProcessor,
                    batchSize) =>
                {
                    batchProcessor(publishedFundingVersionsOne.ToList())
                        .GetAwaiter()
                        .GetResult();

                    batchProcessor(publishedFundingVersionsTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                expectedInterimFilePath,
                fundingLineCode,
                fundingStreamId);

            processedResults
                .Should()
                .BeTrue();

            FileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath,
                        expectedCsvOne,
                        default),
                    Times.Once);

            FileSystemAccess
                .Verify(_ => _.Append(expectedInterimFilePath,
                        expectedCsvTwo,
                        default),
                    Times.Once);
        }
    }
}