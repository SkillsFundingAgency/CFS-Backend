using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        const string CalculationProgressPrependKey = "calculation-progress:";

        [TestMethod]
        public async Task RefreshPublishResults_GivenRequestParametersAreEmpty_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            SpecificationsService specificationsService = CreateService();
            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues("") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenValidRequestParameters_ShouldReturnNoContentResult()
        {
            // Arrange
            const string specificationId = "123";

            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 = new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.NotStarted);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
                cacheProvider: mockCacheProvider);

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues(specificationId) }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<NoContentResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenSpecShouldNotBeRefreshed_CreatesFinishedCacheItemAndReturnsNoContentResult()
        {
            // Arrange
            const string specificationId = "123";

            Specification specification = new Specification
            {
                Id = specificationId,
                LastCalculationUpdatedAt = DateTimeOffset.Now.AddHours(-1).ToLocalTime(),
                PublishedResultsRefreshedAt = DateTimeOffset.Now.ToLocalTime()
            };

            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(specification);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
                cacheProvider: mockCacheProvider);

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues(specificationId) }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<NoContentResult>();
            await mockCacheProvider.Received().SetAsync(Arg.Is($"{CalculationProgressPrependKey}{specificationId}"),
                Arg.Is<SpecificationCalculationExecutionStatus>(m => m.PercentageCompleted == 100 && m.CalculationProgress == CalculationProgressStatus.Finished), TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task RefreshPublishResults_WhenACallToCalculateResultThrowsAnException_ShouldReturnInternalServerError()
        {
            // Arrange
            IMessengerService messengerService = Substitute.For<IMessengerService>();
            messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationsService specificationsService = CreateService(messengerService: messengerService, specificationsRepository: mockSpecificationsRepository);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues("123") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenASpecificationIdThatDoesNotExist_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            const string specificationId = "123";

            IMessengerService messengerService = Substitute.For<IMessengerService>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(specificationId).Returns(new Specification());

            SpecificationsService specificationsService = CreateService(messengerService: messengerService);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues(specificationId) }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task RefreshPublishResults_WhenACallToCalculateResultThrowsAnException_ShouldReportOnCacheWithError()
        {
            // Arrange
            const string specificationId = "123";

            IMessengerService messengerService = Substitute.For<IMessengerService>();
            messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 =
                new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.Error)
                {
                    ErrorMessage = $"Failed to queue publishing of provider results for specification id: {specificationId}"
                };

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationsService specificationsService = CreateService(messengerService: messengerService, specificationsRepository: mockSpecificationsRepository, cacheProvider: mockCacheProvider);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues(specificationId) }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenValidRequestParametersAndFeatureToggleIsFalse_ShouldReturnNoContentResult()
        {
            // Arrange
            const string specificationId1 = "123";
            const string specificationId2 = "333";

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 = new SpecificationCalculationExecutionStatus(specificationId1, 0, CalculationProgressStatus.NotStarted);
            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall2 = new SpecificationCalculationExecutionStatus(specificationId2, 0, CalculationProgressStatus.NotStarted);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
                cacheProvider: mockCacheProvider, featureToggle: featureToggle);

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationIds", new StringValues($"{specificationId1},{specificationId2}") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<NoContentResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId1}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId2}", expectedSpecificationStatusCall2, TimeSpan.FromHours(6), false);

        }

        [TestMethod]
        public async Task RefreshPublishResults_WhenACallToCalculateResultThrowsAnExceptionAndFeatureToggleIsFalse_ShouldReturnInternalServerError()
        {
            // Arrange
            IMessengerService messengerService = Substitute.For<IMessengerService>();
            messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            SpecificationsService specificationsService = CreateService(messengerService: messengerService, specificationsRepository: mockSpecificationsRepository, featureToggle: featureToggle);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationIds", new StringValues("123, 333, 422, 122") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenASpecificationIdThatDoesNotExistAndFeatureToggleIsFalse_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            const string validSpecificationId = "123";
            const string invalidSpecificationId = "333";

            IMessengerService messengerService = Substitute.For<IMessengerService>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(validSpecificationId).Returns(new Specification());

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            SpecificationsService specificationsService = CreateService(messengerService: messengerService, featureToggle: featureToggle);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationIds", new StringValues($"{validSpecificationId}, {invalidSpecificationId}") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task RefreshPublishResults_WhenACallToCalculateResultThrowsAnExceptionAndFeatureToggleIsFalse_ShouldReportOnCacheWithError()
        {
            // Arrange
            const string specificationId = "123";

            IMessengerService messengerService = Substitute.For<IMessengerService>();
            messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 =
                new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.Error)
                {
                    ErrorMessage = $"Failed to queue publishing of provider results for specification id: {specificationId}"
                };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationsService specificationsService = CreateService(messengerService: messengerService, specificationsRepository: mockSpecificationsRepository, cacheProvider: mockCacheProvider, featureToggle: featureToggle);
            HttpRequest httpRequest = Substitute.For<HttpRequest>();
            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationIds", new StringValues(specificationId) }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenSingleSpecificationIdAndUseJobServiceIsTrue_ShouldReturnNoContentResult()
        {
            // Arrange
            const string specificationId1 = "123";

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 = new SpecificationCalculationExecutionStatus(specificationId1, 0, CalculationProgressStatus.NotStarted);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
                cacheProvider: mockCacheProvider, featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationId", new StringValues($"{specificationId1}") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<NoContentResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId1}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);

            await jobsApiClient.Received(1).CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.PublishProviderResultsJob && j.SpecificationId == specificationId1 && j.Trigger.Message == $"Refreshing published provider results for specification"));
        }

        [TestMethod]
        public async Task RefreshPublishResults_GivenMultipleSpecificationIdsAndUseJobServiceIsTrue_ShouldReturnNoContentResult()
        {
            // Arrange
            const string specificationId1 = "123";
            const string specificationId2 = "333";

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);
            featureToggle
                .IsJobServiceForPublishProviderResultsEnabled()
                .Returns(true);

            HttpRequest httpRequest = Substitute.For<HttpRequest>();

            ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
            mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 = new SpecificationCalculationExecutionStatus(specificationId1, 0, CalculationProgressStatus.NotStarted);
            SpecificationCalculationExecutionStatus expectedSpecificationStatusCall2 = new SpecificationCalculationExecutionStatus(specificationId2, 0, CalculationProgressStatus.NotStarted);

            ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
                cacheProvider: mockCacheProvider, featureToggle: featureToggle, jobsApiClient: jobsApiClient);

            httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
            {
                { "specificationIds", new StringValues($"{specificationId1},{specificationId2}") }
            }));

            // Act
            IActionResult actionResultReturned = await specificationsService.RefreshPublishedResults(httpRequest);

            // Assert
            actionResultReturned.Should().BeOfType<NoContentResult>();
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId1}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
            await mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId2}", expectedSpecificationStatusCall2, TimeSpan.FromHours(6), false);

            await jobsApiClient.Received(1).CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.PublishProviderResultsJob && j.SpecificationId == specificationId1 && j.Trigger.Message == $"Refreshing published provider results for specification"));
            await jobsApiClient.Received(1).CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.PublishProviderResultsJob && j.SpecificationId == specificationId2 && j.Trigger.Message == $"Refreshing published provider results for specification"));
        }
    }
}
