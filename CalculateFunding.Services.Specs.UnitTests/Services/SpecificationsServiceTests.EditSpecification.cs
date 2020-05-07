using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;


namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        private readonly IQueryCollection _queryStringValues = 
            new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });
        private readonly PolicyModels.FundingPeriod _fundingPeriod = new PolicyModels.FundingPeriod
        {
            Id = "fp10",
            Name = "fp 10"
        };
        private readonly ApiResponse<PolicyModels.FundingPeriod> _fundingPeriodResponse;

        public SpecificationsServiceTests()
        {
            _specificationsRepository = CreateSpecificationsRepository();
            _specification = CreateSpecification();
            _policiesApiClient = CreatePoliciesApiClient();
            _mapper = CreateImplementedMapper();
            _searchRepository = CreateSearchRepository();
            _cacheProvider = CreateCacheProvider();
            _messengerService = CreateMessengerService();
            _versionRepository = CreateVersionRepository();
            _providersApiClient = CreateProvidersApiClient();
            _jobManagement = CreateJobManagement();
            _fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(
                HttpStatusCode.OK, _fundingPeriod);
        }

        [TestMethod]
        public async Task EditSpecification_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            SpecificationsService service = CreateService(logs: _logger);

            IActionResult result = await service.EditSpecification(null, null, null, null);

            result.Should().BeOfType<BadRequestObjectResult>();
            _logger.Received(1)
                .Error(Arg.Is("No specification Id was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            SpecificationsService service = CreateService(logs: _logger);

            IActionResult result = await service.EditSpecification(SpecificationId, null, null, null);

            result.Should().BeOfType<BadRequestObjectResult>();
            _logger.Received(1)
                .Error(Arg.Is("No edit modeld was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_WhenInvalidModelProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult()
            {
                Errors = { new ValidationFailure("error", "error") }
            };
            IValidator<SpecificationEditModel> validator = CreateEditSpecificationValidator(validationResult);
            SpecificationsService specificationsService = CreateService(specificationEditModelValidator: validator);
            SpecificationEditModel specificationEditModel = new SpecificationEditModel();

            // Act
            IActionResult result = await specificationsService.EditSpecification(SpecificationId, specificationEditModel, null, null);

            // Assert
            result
                .Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().BeOfType<SerializableError>()
                .Which.Should().HaveCount(1);
            await validator
                .Received(1).ValidateAsync(Arg.Any<SpecificationEditModel>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel();
            _specificationsRepository.GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);
            SpecificationsService service = CreateService(logs: _logger, specificationsRepository: _specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            result
                .Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should()
                .Be("Specification not found");
            _logger
                .Received(1)
                .Warning(Arg.Is($"Failed to find specification for id: {SpecificationId}"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasfoundAndFundingPeriodChangedButFailedToGetFundingPeriodsFromCosmos_ReturnsPreConditionFailedresult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            _specificationsRepository.GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(_specification);
            SpecificationsService service = CreateService(logs: _logger, specificationsRepository: _specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            result
                .Should().BeOfType<PreconditionFailedResult>()
                .Which.Value.Should().Be($"Unable to find funding period with ID '{specificationEditModel.FundingPeriodId}'.");
        }

        [TestMethod]
        public async Task EditSpecification_GivenFailsToUpdateCosmosWithBadRequest_ReturnsBadRequest()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id,
                Name = existingFundingStreams.First().Name
            };
            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse =
                new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);
            _specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(_specification);
            _policiesApiClient
                .GetFundingPeriodById(Arg.Is(_fundingPeriod.Id))
                .Returns(_fundingPeriodResponse);
            _policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStream.Id))
                .Returns(fundingStreamResponse);
            _specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.BadRequest);
            SpecificationsService service = CreateService(mapper: _mapper, logs: _logger, specificationsRepository: _specificationsRepository, policiesApiClient: _policiesApiClient);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);
        }

        [TestMethod]
        public async Task EditSpecification_GivenProviderVersionChanges_CallsRegenerateScopedProviders()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name"
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.ProviderVersionId = "Provider version 2";

            _providersApiClient.RegenerateProviderSummariesForSpecification(_specification.Id, true)
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Arrange
            await
                _searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == "new spec name" &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));
            await
                _cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{_specification.Id}"));
            await
                _messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == "new spec name" &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>(), Arg.Is(true));
            await
              _versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public void EditSpecification_GivenProviderVersionChangesAndRegenerateScopedProvidersFails_ExceptionThrown()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name"
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.ProviderVersionId = "Provider version 2";

            _providersApiClient.RegenerateProviderSummariesForSpecification(_specification.Id, true)
                .Returns(new ApiResponse<bool>(HttpStatusCode.BadRequest));

            _jobManagement.QueueJobAndWait(Arg.Any<Func<Task<bool>>>() , Arg.Is<string>(JobConstants.DefinitionNames.PopulateScopedProvidersJob), _specification.Id, "correlationId", "topic")
                .Returns(false);

            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            Func<Task> invocation = async () => await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Arrange
            invocation.Should()
                .Throw<RetriableException>()
                .WithMessage($"Unable to re-generate scoped providers while editing specification '{_specification.Id}' with status code: {HttpStatusCode.BadRequest}");
        }


        [TestMethod]
        public async Task EditSpecification_GivenChanges_UpdatesSearchAndSendsMessage()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };

            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Arrange
            await
                _searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == "new spec name" &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));
            await
                _cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{_specification.Id}"));
            await
                _messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == "new spec name" &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>(), Arg.Is(true));
            await
              _versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        private SpecificationsService CreateSpecificationsService(PolicyModels.FundingStream fundingStream, Models.Specs.SpecificationVersion newSpecVersion)
        {
            _specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(_specification);
            _policiesApiClient
                .GetFundingPeriodById(Arg.Is(_fundingPeriod.Id))
                .Returns(_fundingPeriodResponse);
            _specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);
            _versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);
            SpecificationsService service = CreateService(
                mapper: _mapper, logs: _logger, specificationsRepository: _specificationsRepository,
                policiesApiClient: _policiesApiClient, searchRepository: _searchRepository,
                cacheProvider: _cacheProvider, messengerService: _messengerService,
                specificationVersionRepository: _versionRepository,
                providersApiClient: _providersApiClient, jobManagement: _jobManagement);
            return service;
        }

        [DataTestMethod]
        [DataRow("new spec name", "new spec name")]
        [DataRow("   new spec name   ", "new spec name")]
        public async Task EditSpecification_GivenChanges_CreatesNewVersionWithTrimmedName(string specificationEditModelName, string resultSpecificationName)
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = specificationEditModelName,
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name.Trim();
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            _versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);
            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            await
                _searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == resultSpecificationName &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));
            await
                _cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{_specification.Id}"));
            await
                _messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == resultSpecificationName &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>(), Arg.Is(true));
            await
              _versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesAndSpecContainsCalculations_UpdatesSearchAndSendsMessage()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            _versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);
            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            await
                _searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                            m => m.First().Id == SpecificationId &&
                            m.First().Name == "new spec name" &&
                            m.First().FundingPeriodId == "fp10" &&
                            m.First().FundingStreamIds.Count() == 1
                        ));
            await
                _cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{_specification.Id}"));
            await
                _messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId &&
                                    m.Current.Name == "new spec name" &&
                                    m.Previous.Name == "Spec name"
                                    ), Arg.Any<IDictionary<string, string>>(), Arg.Is(true));
            await
               _versionRepository
                .Received(1)
                .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public void EditSpecification_WhenIndexingReturnsErrors_ShouldThrowException()
        {
            //Arrange
            var existingFundingStreams = _specification.Current.FundingStreams;
            const string errorMessage = "Encountered error 802 code";
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = existingFundingStreams.First().Id
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            _searchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(new[] { new IndexError() { ErrorMessage = errorMessage } });
            var service = CreateSpecificationsService(fundingStream, newSpecVersion);

            //Act
            Func<Task<IActionResult>> editSpecification = async () => await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            editSpecification
                .Should()
                .Throw<ApplicationException>()
                .Which
                .Message
                .Should()
                .Be($"Could not index specification {_specification.Current.Id} because: {errorMessage}");
        }

        private static MemoryStream CreateMemoryStreamForModel(SpecificationEditModel specificationEditModel)
        {
            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            return new MemoryStream(byteArray);
        }
    }
}
