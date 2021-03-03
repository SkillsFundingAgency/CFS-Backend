using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ExpandoObject = System.Dynamic.ExpandoObject;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedFundingOrganisationGroupCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedFundingOrganisationGroupCsvBatchProcessor(FileSystemAccess.Object,
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
        [DataRow(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, false)]
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
            RandomString fundingStreamId = NewRandomString();

            GivenThePublishedFundingForBatchProfessingFeed(specificationId, 
                fundingStreamId, 
                fundingPeriodId, 
                new Mock<ICosmosDbFeedIterator<PublishedFunding>>().Object);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                NewRandomString(),
                NewRandomString(),
                fundingStreamId);

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

            IEnumerable<PublishedFunding> publishedFundingOne = new[]
            {
                NewPublishedFunding()
            };
            IEnumerable<PublishedFunding> publishedFundingTwo = new[]
            {
                NewPublishedFunding(), NewPublishedFunding(), NewPublishedFunding()
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

            Mock<ICosmosDbFeedIterator<PublishedFunding>> feed = new Mock<ICosmosDbFeedIterator<PublishedFunding>>();

            GivenTheCsvRowTransformation(publishedFundingOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedFundingTwo, transformedRowsTwo, expectedCsvTwo, false);
            AndThePublishedFundingForBatchProfessingFeed(specificationId, fundingStreamId, fundingPeriodId, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishedFundingOne, publishedFundingTwo);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues,
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

        private void GivenThePublishedFundingForBatchProfessingFeed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            ICosmosDbFeedIterator<PublishedFunding> feed)
            => AndThePublishedFundingForBatchProfessingFeed(specificationId,
                fundingStreamId,
                fundingPeriodId,
                feed);

        private void AndThePublishedFundingForBatchProfessingFeed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            ICosmosDbFeedIterator<PublishedFunding> feed)
            => PublishedFunding.Setup(_ => _.GetPublishedFundingForBatchProcessing(specificationId,
                    fundingStreamId,
                    fundingPeriodId,
                    CsvBatchProcessBase.BatchSize))
                .Returns(feed);
    }
}