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
        [DataRow(FundingLineCsvGeneratorJobType.ProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, true)]
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

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, NewRandomString());

            processedResults
                .Should()
                .BeFalse();
        }
        
        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState)]
        [DataRow(FundingLineCsvGeneratorJobType.Released)]
        public async Task TransformsPublishedProvidersForSpecificationInBatchesAndCreatesCsvWithResults(
            FundingLineCsvGeneratorJobType jobType)
        {
            string specificationId = NewRandomString();
            string expectedInterimFilePath = "TODO;";
            
            IEnumerable<PublishedProvider> publishProvidersOne = new []
            {
                new PublishedProvider(),
            };
            IEnumerable<PublishedProvider> publishedProvidersTwo = new []
            {
                new PublishedProvider(),
                new PublishedProvider(),
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

            GivenTheCsvRowTransformation(publishProvidersOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedProvidersTwo, transformedRowsTwo, expectedCsvTwo,  false);
            AndThePredicate(jobType, predicate);
            
            PublishedFunding.Setup(_ => _.PublishedProviderBatchProcessing(predicate,
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100))
                .Callback<string, string, Func<List<PublishedProvider>, Task>, int>((pred, spec,  batchProcessor, batchSize) =>
                {
                    batchProcessor(publishProvidersOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProvidersTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);

            bool processedResults = await WhenTheCsvIsGenerated(jobType, specificationId, expectedInterimFilePath);

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