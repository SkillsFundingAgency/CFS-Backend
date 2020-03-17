using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedProviderCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedProviderCsvBatchProcessor(PublishedFunding.Object,
                PredicateBuilder.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object);
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, true)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, true)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, true)]
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

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, NewRandomString(), NewRandomString());

            processedResults
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState)]
        [DataRow(FundingLineCsvGeneratorJobType.Released)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string expectedInterimFilePath = NewRandomString();
            
            IEnumerable<PublishedProvider> publishProvidersOne = new []
            {
                NewPublishedProvider(),
            };
            IEnumerable<PublishedProvider> publishedProvidersTwo = new []
            {
                NewPublishedProvider(),
                NewPublishedProvider(),
                NewPublishedProvider()
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

            GivenTheCsvRowTransformation(publishProvidersOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedProvidersTwo, transformedRowsTwo, expectedCsvTwo,  false);
            AndThePredicate(jobType, predicate);
            AndTheJoinPredicate(jobType, joinPredicate);

            PublishedFunding.Setup(_ => _.PublishedProviderBatchProcessing(predicate,
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100,
                    joinPredicate,
                    fundingLineCode))
                .Callback<string, string, Func<List<PublishedProvider>, Task>, int, string, string>((pred, spec,  batchProcessor, batchSize, joinPred, flc) =>
                {
                    batchProcessor(publishProvidersOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProvidersTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, expectedInterimFilePath, fundingLineCode);

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
        
        private PublishedProvider NewPublishedProvider() => new PublishedProvider();
    }
}