using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishedFundingCsvJobsServiceTest
    {
        private IPublishedFundingCsvJobsService _publishedFundingCsvJobsService;
        private Mock<IPublishedFundingDataService> _publishedFundingDataService;
        private Mock<IGeneratePublishedFundingCsvJobsCreationLocator> _generateCsvJobsLocator;
        private Mock<ISpecificationService> _specificationService;
        private Mock<IGeneratePublishedFundingCsvJobsCreation> _generatePublishedFundingCsvJobsCreation;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingDataService = new Mock<IPublishedFundingDataService>();
            _generateCsvJobsLocator = new Mock<IGeneratePublishedFundingCsvJobsCreationLocator>();
            _specificationService = new Mock<ISpecificationService>();
            _generatePublishedFundingCsvJobsCreation = new Mock<IGeneratePublishedFundingCsvJobsCreation>();

            _publishedFundingCsvJobsService = new PublishedFundingCsvJobsService(_publishedFundingDataService.Object, 
                _generateCsvJobsLocator.Object,
                _specificationService.Object);
        }

        [TestMethod]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Approve)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Refresh)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Release)]
        public async Task QueueCsvJobs_WhenQueueJobForCsvRequested_SuccessfullyQueuesCsvJobs(GeneratePublishingCsvJobsCreationAction actionType)
        {
            string specificationId = new RandomString();
            string fundingPeriodId = new RandomString();
            string fundingStreamId = new RandomString();
            string correlationId = new RandomString();

            GivenSpecification(specificationId, fundingPeriodId, fundingStreamId);
            AndReportGenerator(actionType);
            await WhenQueueJobForCsvRequested(actionType, specificationId, correlationId);
            AndTheCsvGenerationJobsWereCreated(actionType, 
                specificationId, 
                fundingPeriodId,
                actionType == GeneratePublishingCsvJobsCreationAction.Release ? new[] { fundingStreamId } : Array.Empty<string>());
        }

        [TestMethod]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Approve)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Refresh)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Release)]
        public async Task GenerateCsvJobs_WhenQueueJobForCsvRequested_SuccessfullyGeneratesCsvJobs(GeneratePublishingCsvJobsCreationAction actionType)
        {
            string specificationId = new RandomString();
            string fundingPeriodId = new RandomString();
            string fundingStreamId = new RandomString();
            string correlationId = new RandomString();

            AndReportGenerator(actionType);
            await WhenGenerateJobForCsvRequested(actionType, 
                specificationId, 
                fundingPeriodId, 
                actionType == GeneratePublishingCsvJobsCreationAction.Release ? new[] { fundingStreamId } : null, correlationId);

            AndTheCsvGenerationJobsWereCreated(actionType, 
                specificationId, 
                fundingPeriodId, 
                actionType == GeneratePublishingCsvJobsCreationAction.Release ? new[] { fundingStreamId } : Array.Empty<string>());
        }

        [TestMethod]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Approve)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Refresh)]
        [DataRow(GeneratePublishingCsvJobsCreationAction.Release)]
        public void QueueCsvJobs_WhenInvalidSpecificationSupplied_ThrowsException(GeneratePublishingCsvJobsCreationAction actionType)
        {
            string specificationId = new RandomString();
            string correlationId = new RandomString();

            Func<Task> invocation = () => WhenQueueJobForCsvRequested(actionType, specificationId, correlationId);

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage($"Could not find specification with id '{specificationId}'");
        }

        private void GivenSpecification(string specificationId, string fundingPeriodId, string fundingStreamId)
        {
            _specificationService
                .Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(NewSpecification(_ => _.WithId(specificationId)
                            .WithFundingPeriodId(fundingPeriodId)
                            .WithFundingStreamIds(new[] { fundingStreamId })));
        }

        private void AndReportGenerator(GeneratePublishingCsvJobsCreationAction actionType)
        {
            _generateCsvJobsLocator.Setup(_ => _.GetService(actionType))
                .Returns(_generatePublishedFundingCsvJobsCreation.Object);
        }

        private void AndTheCsvGenerationJobsWereCreated(GeneratePublishingCsvJobsCreationAction actionType, 
            string specificationId, 
            string fundingPeriodId, 
            IEnumerable<string> fundingStreamIds)
        {
            _generateCsvJobsLocator.Verify(_ =>
                _.GetService(actionType),
                Times.Once);

            _generatePublishedFundingCsvJobsCreation
                .Verify(_ => _.CreateJobs(It.Is<PublishedFundingCsvJobsRequest>(job =>
                                                                                    job.SpecificationId == specificationId &&
                                                                                    job.FundingPeriodId == fundingPeriodId &&
                                                                                    job.FundingStreamIds.SequenceEqual(fundingStreamIds))));
        }

        private async Task WhenGenerateJobForCsvRequested(GeneratePublishingCsvJobsCreationAction actionType, 
            string specificationId, 
            string fundingPeriodId, 
            IEnumerable<string> fundingStreamIds, 
            string correlationId)
        {
            await _publishedFundingCsvJobsService.GenerateCsvJobs(actionType, specificationId, fundingPeriodId, correlationId, null, fundingStreamIds);
        }

        private async Task WhenQueueJobForCsvRequested(GeneratePublishingCsvJobsCreationAction actionType, 
            string specificationId, 
            string correlationId)
        {
            await _publishedFundingCsvJobsService.QueueCsvJobs(actionType, specificationId, correlationId, null);
        }

        private SpecificationSummary NewSpecification(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }
    }
}