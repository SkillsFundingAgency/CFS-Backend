using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedProviderVersionCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedProviderVersionCsvBatchProcessor(PublishedFunding.Object,
                PredicateBuilder.Object,
                FileSystemAccess.Object,
                CsvUtils.Object);
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, true)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
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
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            
            GivenThePublishedProviderVersionsForBatchProfessingFeed(specificationId, fundingLineCode, new Mock<ICosmosDbFeedIterator<PublishedProviderVersion>>().Object);

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, fundingPeriodId, NewRandomString(), fundingLineCode, fundingStreamId);

            processedResults
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState)]
        [DataRow(FundingLineCsvGeneratorJobType.Released)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string expectedInterimFilePath = NewRandomString();
            
            IEnumerable<PublishedProviderVersion> publishProviderVersionsOne = new []
            {
                NewPublishedProviderVersion(),
            };
            IEnumerable<PublishedProviderVersion> publishedProviderVersionTwo = new []
            {
                NewPublishedProviderVersion(),
                NewPublishedProviderVersion()
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

            Mock<ICosmosDbFeedIterator<PublishedProviderVersion>> feed = new Mock<ICosmosDbFeedIterator<PublishedProviderVersion>>();

            GivenTheCsvRowTransformation(publishProviderVersionsOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedProviderVersionTwo, transformedRowsTwo, expectedCsvTwo,  false);
            AndThePredicate(jobType, predicate);
            AndTheJoinPredicate(jobType, joinPredicate);
            AndThePublishedProviderVersionsForBatchProfessingFeed(predicate, joinPredicate, specificationId, fundingLineCode, feed.Object);
            AndTheFeedIteratorHasThePages(feed, publishProviderVersionsOne, publishedProviderVersionTwo);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, fundingPeriodId, expectedInterimFilePath, fundingLineCode, null);

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
        
        private PublishedProviderVersion NewPublishedProviderVersion() => new PublishedProviderVersion();
        
        private void GivenThePublishedProviderVersionsForBatchProfessingFeed(string specificationId,
            string fundingLineCode,
            ICosmosDbFeedIterator<PublishedProviderVersion> feed)
            => PublishedFunding.Setup(_ => _.GetPublishedProviderVersionsForBatchProcessing(It.IsAny<string>(),
                    specificationId,
                    CsvBatchProcessBase.BatchSize,
                    It.IsAny<string>(),
                    fundingLineCode))
                .Returns(feed);

        private void AndThePublishedProviderVersionsForBatchProfessingFeed(string predicate,
            string joinPredicate,
            string specificationId,
            string fundingLineCode,
            ICosmosDbFeedIterator<PublishedProviderVersion> feed)
            => PublishedFunding.Setup(_ => _.GetPublishedProviderVersionsForBatchProcessing(predicate,
                    specificationId,
                    CsvBatchProcessBase.BatchSize,
                    joinPredicate,
                    fundingLineCode))
                .Returns(feed);
    }
}