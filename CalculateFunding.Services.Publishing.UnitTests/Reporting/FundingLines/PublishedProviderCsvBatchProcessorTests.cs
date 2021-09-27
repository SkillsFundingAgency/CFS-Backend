using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
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
        private Mock<IProfilingService> _profilingService;

        [TestInitialize]
        public void SetUp()
        {
            _profilingService = new Mock<IProfilingService>();

            BatchProcessor = new PublishedProviderCsvBatchProcessor(PublishedFunding.Object,
                PredicateBuilder.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _profilingService.Object,
                FileSystemAccess.Object,
                CsvUtils.Object,
                PoliciesService.Object);
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
            string fundingPeriodId = NewRandomString();

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, fundingPeriodId, NewRandomString(), NewRandomString(), NewRandomString(), NewRandomString());

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
            string fundingPeriodId = NewRandomString();
            string fundingLineName = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingStreamId = NewRandomString();

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
            string groupingPredicate = NewRandomString();

            GivenTheCsvRowTransformation<PublishedProvider>(publishedProviders =>
            {
                return publishedProviders.SequenceEqual(publishProvidersOne);
            }, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation<PublishedProvider>(publishedProviders =>
            {
                return publishedProviders.SequenceEqual(publishedProvidersTwo);
            }, transformedRowsTwo, expectedCsvTwo,  false);
            AndThePredicate(jobType, predicate);
            AndTheJoinPredicate(jobType, joinPredicate);

            PublishedFunding.Setup(_ => _.PublishedProviderBatchProcessing(predicate,
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100,
                    joinPredicate,
                    fundingLineName))
                .Callback<string, string, Func<List<PublishedProvider>, Task>, int, string, string>((pred, spec, 
                    batchProcessor, batchSize, joinPred, flc) =>
                {
                    batchProcessor(publishProvidersOne.ToList())
                        .GetAwaiter()
                        .GetResult();

                    batchProcessor(publishedProvidersTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, fundingPeriodId, expectedInterimFilePath, fundingLineName, fundingStreamId, fundingLineCode);

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
        
        private PublishedProvider NewPublishedProvider() => new PublishedProvider { Current = new PublishedProviderVersion { FundingLines = new[] { new FundingLine { Name = "FLName"} } } };
    }
}