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
    public class PublishedProviderVersionCsvBatchProcessorTests : BatchProcessorTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            BatchProcessor = new PublishedProviderVersionCsvBatchProcessor(PublishedFunding.Object,
                PredicateBuilder.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                FileSystemAccess.Object,
                CsvUtils.Object);
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.History, true)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.ProfileValues, false)]
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

            bool processedResults = await WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType.Released, specificationId, NewRandomString(), fundingLineCode);

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
            string fundingLineCode = NewRandomString();
            string expectedInterimFilePath = "TODO;";
            
            IEnumerable<PublishedProviderVersion> publishProviderVersionsOne = new []
            {
                new PublishedProviderVersion(),
            };
            IEnumerable<PublishedProviderVersion> publishedProviderVersionTwo = new []
            {
                new PublishedProviderVersion(),
                new PublishedProviderVersion(),
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

            GivenTheCsvRowTransformation(publishProviderVersionsOne, transformedRowsOne, expectedCsvOne, true);
            AndTheCsvRowTransformation(publishedProviderVersionTwo, transformedRowsTwo, expectedCsvTwo,  false);
            
            PublishedFunding.Setup(_ => _.PublishedProviderVersionBatchProcessing(specificationId,
                    It.IsAny<Func<List<PublishedProviderVersion>, Task>>(),
                    100))
                .Callback<string, Func<List<PublishedProviderVersion>, Task>, int>((spec,  batchProcessor, batchSize) =>
                {
                    batchProcessor(publishProviderVersionsOne.ToList())
                        .GetAwaiter()
                        .GetResult();
                    
                    batchProcessor(publishedProviderVersionTwo.ToList())
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
    }
}