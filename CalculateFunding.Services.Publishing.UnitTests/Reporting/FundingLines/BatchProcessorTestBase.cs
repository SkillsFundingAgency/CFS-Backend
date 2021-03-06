using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    public abstract class BatchProcessorTestBase
    {
        protected Mock<IPublishedFundingPredicateBuilder> PredicateBuilder;
        protected Mock<ICsvUtils> CsvUtils;
        protected Mock<IPublishedFundingRepository> PublishedFunding;
        protected Mock<IFileSystemAccess> FileSystemAccess;
        protected IFundingLineCsvBatchProcessor BatchProcessor;
        
        private Mock<IFundingLineCsvTransform> _transformation;


        [TestInitialize]
        public void BatchProcessorTestBaseSetUp()
        {
            PredicateBuilder = new Mock<IPublishedFundingPredicateBuilder>();
            PublishedFunding = new Mock<IPublishedFundingRepository>();
            CsvUtils = new Mock<ICsvUtils>();
            FileSystemAccess = new Mock<IFileSystemAccess>();
            
            _transformation = new Mock<IFundingLineCsvTransform>();
        }
        
        protected async Task<bool> WhenTheCsvIsGenerated(FundingLineCsvGeneratorJobType jobType, 
            string specificationId, 
            string fundingPeriodId,
            string path,
            string fundingLineCode,
            string fundingStreamId)
        {
            return await BatchProcessor.GenerateCsv(jobType, specificationId, fundingPeriodId, path, _transformation.Object, fundingLineCode, fundingStreamId);
        }

        protected void AndTheCsvRowTransformation(IEnumerable<dynamic> publishedProviders, ExpandoObject[] transformedRows, string csv, bool outputHeaders)
        {
            GivenTheCsvRowTransformation(publishedProviders, transformedRows, csv, outputHeaders);
        }

        protected void GivenTheCsvRowTransformation(IEnumerable<dynamic> publishedProviders, IEnumerable<ExpandoObject> transformedRows, string csv, bool outputHeaders)
        {
            _transformation
                .Setup(_ => _.Transform(publishedProviders))
                .Returns(transformedRows);

            CsvUtils
                .Setup(_ => _.AsCsv(transformedRows, outputHeaders))
                .Returns(csv);
        }

        protected void AndThePredicate(FundingLineCsvGeneratorJobType jobType, string predicate)
        {
            PredicateBuilder.Setup(_ => _.BuildPredicate(jobType))
                .Returns(predicate);
        }

        protected void AndTheJoinPredicate(FundingLineCsvGeneratorJobType jobType, string predicate)
        {
            PredicateBuilder.Setup(_ => _.BuildJoinPredicate(jobType))
                .Returns(predicate);
        }

        protected static RandomString NewRandomString()
        {
            return new RandomString();
        }

        protected static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected static PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        protected static PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }
    }
}