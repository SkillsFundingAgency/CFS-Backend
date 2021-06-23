using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedFundingVersionOrganisationGroupCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedFundingVersionOrganisationGroupCsvBatchProcessor(FileSystemAccess.Object,
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
            RandomString fundingStreamId = NewRandomString();
            
            GivenThePublishedFundingVersionForBatchProfessingFeed(specificationId, 
                fundingStreamId, 
                fundingPeriodId, 
                new Mock<ICosmosDbFeedIterator>().Object);
            
            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                NewRandomString(),
                NewRandomString(),
                fundingStreamId,
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
            string fundingLineName = NewRandomString();
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

            Mock<ICosmosDbFeedIterator> feed = new Mock<ICosmosDbFeedIterator>();

            GivenTheCsvRowTransformation<PublishedFundingVersion>(publishedProviders =>
            {
                return publishedProviders == publishedFundingVersionsOne;
            }, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation<PublishedFundingVersion>(publishedProviders =>
            {
                return publishedProviders == publishedFundingVersionsTwo;
            }, transformedRowsTwo, expectedCsvTwo, false);
            AndThePublishedFundingVersionForBatchProfessingFeed(specificationId, fundingStreamId, fundingPeriodId, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishedFundingVersionsOne, publishedFundingVersionsTwo);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                specificationId,
                fundingPeriodId,
                expectedInterimFilePath,
                fundingLineName,
                fundingStreamId,
                fundingLineCode);

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
        
        private void GivenThePublishedFundingVersionForBatchProfessingFeed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            ICosmosDbFeedIterator feed)
            => AndThePublishedFundingVersionForBatchProfessingFeed(specificationId,
                fundingStreamId,
                fundingPeriodId,
                feed);

        private void AndThePublishedFundingVersionForBatchProfessingFeed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            ICosmosDbFeedIterator feed)
            => PublishedFunding.Setup(_ => _.GetPublishedFundingVersionsForBatchProcessing(specificationId,
                    fundingStreamId,
                    fundingPeriodId,
                    CsvBatchProcessBase.BatchSize))
                .Returns(feed);
    }
}