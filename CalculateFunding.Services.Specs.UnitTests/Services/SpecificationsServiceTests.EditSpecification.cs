﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using SpecificationVersion = CalculateFunding.Models.Specs.SpecificationVersion;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        private readonly PolicyModels.FundingPeriod _fundingPeriod = new PolicyModels.FundingPeriod
        {
            Id = "fp10",
            Name = "fp 10",
            Period = "p10"
        };
        private readonly ApiResponse<PolicyModels.FundingPeriod> _fundingPeriodResponse;

        IQueueEditSpecificationJobActions _editSpecificationJobActions;

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
            _editSpecificationJobActions = CreateQueueEditSpecificationJobActions();
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
        public async Task EditSpecification_GivenSpecificationWasfoundAndFundingPeriodChangedButFailedToGetFundingPeriodsFromCosmos_ReturnsPreConditionFailedResult()
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
            IEnumerable<Reference> existingFundingStreams = _specification.Current.FundingStreams;
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

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId);

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
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name"
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.ProviderVersionId = "Provider version 2";

            _providersApiClient.RegenerateProviderSummariesForSpecification(_specification.Id, true)
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

            var service = CreateSpecificationsService(newSpecVersion);

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
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                Description = "new spec description",
                AssignedTemplateIds =  new Dictionary<string, string>()
            };

            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);
            
            string correlationId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = _specification.Current;

            //Act
            await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

            await _templateVersionChangedHandler
                .Received(1)
                .HandleTemplateVersionChanged(Arg.Is(previousSpecificationVersion),
                    Arg.Is<SpecificationVersion>(_ => newSpecVersion.AsJson(true) == _.AsJson(true)),
                    Arg.Is(specificationEditModel.AssignedTemplateIds), 
                    Arg.Is(user),
                     Arg.Is(correlationId));
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesWithFDZProviderSourceAndProviderSnapshotIdNotSet_ReturnsPreconditionFailedResult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                Description = "new spec description",
                AssignedTemplateIds = new Dictionary<string, string>()
            };

            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId,
                ProviderSource.FDZ);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            string correlationId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = _specification.Current;

            //Act
            IActionResult actionResult =
                await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            actionResult
                .Should()
                .BeOfType<PreconditionFailedResult>();
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesWithFDZProviderSource_UpdatesSearchAndSendsMessage()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                Description = "new spec description",
                AssignedTemplateIds = new Dictionary<string, string>(),
                ProviderSnapshotId = 1
            };

            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;
            newSpecVersion.ProviderSnapshotId = 1;
            newSpecVersion.ProviderSource = Models.Providers.ProviderSource.FDZ;

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId,
                ProviderSource.FDZ);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            string correlationId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = _specification.Current;

            //Act
            await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

            await _templateVersionChangedHandler
                .Received(1)
                .HandleTemplateVersionChanged(Arg.Is(previousSpecificationVersion),
                    Arg.Is<SpecificationVersion>(_ => newSpecVersion.AsJson(true) == _.AsJson(true)),
                    Arg.Is(specificationEditModel.AssignedTemplateIds),
                    Arg.Is(user),
                     Arg.Is(correlationId));
        }

        [TestMethod]
        public async Task EditSpecification_GivenChanges_QueueEditSpecificationJobActions()
        {
            //Arrange
            bool withRunCalculationEngineAfterCoreProviderUpdate = true;

            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                AssignedTemplateIds = new Dictionary<string, string>()
            };
            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;

            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId,
                withRunCalculationEngineAfterCoreProviderUpdate: withRunCalculationEngineAfterCoreProviderUpdate);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);
          
            string correlationId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = _specification.Current;

            //Act
            await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

            await _templateVersionChangedHandler
                .Received(1)
                .HandleTemplateVersionChanged(Arg.Is(previousSpecificationVersion),
                    Arg.Any<SpecificationVersion>(),
                    Arg.Is(specificationEditModel.AssignedTemplateIds),
                    Arg.Is(user),
                     Arg.Is(correlationId));

            await _editSpecificationJobActions
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId)  &&
                             m.Name == specificationEditModel.Name),
                    Arg.Any<Reference>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    withRunCalculationEngineAfterCoreProviderUpdate);
        }

        [TestMethod]
        public async Task EditSpecification_GivenSetLatestProviderVersionChangesFromManualToUseLatest_InstructToQueueProviderSnapshotDataLoadJob()
        {
            //Arrange
            _specification.Current.CoreProviderVersionUpdates = CoreProviderVersionUpdates.Manual;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                AssignedTemplateIds = new Dictionary<string, string>(),
                CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest
            };
            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;

            string specFundingStreamId = _specification.Current.FundingStreams.FirstOrDefault().Id;

            AndGetFundingConfiguration(
                specFundingStreamId,
                specificationEditModel.FundingPeriodId);

            _providersApiClient.GetCurrentProviderMetadataForFundingStream(specFundingStreamId)
                .Returns(new ApiResponse<CurrentProviderVersionMetadata>(HttpStatusCode.NotFound, null));

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            string correlationId = NewRandomString();

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            result.Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"No current provider metadata returned for funding stream id '{specFundingStreamId}'.");
        }

        [TestMethod]
        public async Task EditSpecification_GivenChangesAndNoCurrentProviderVersionMetatdataForFundingStream_ReturnServerError()
        {
            //Arrange
            bool withRunCalculationEngineAfterCoreProviderUpdate = true;
            _specification.Current.CoreProviderVersionUpdates = CoreProviderVersionUpdates.Manual;
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId,
                AssignedTemplateIds = new Dictionary<string, string>(),
                CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest
            };
            Reference user = new Reference();

            SpecificationVersion newSpecVersion = _specification.Current.DeepCopy(useCamelCase: false);
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            newSpecVersion.FundingPeriod.Name = "p10";
            newSpecVersion.Author = user;
            newSpecVersion.Description = specificationEditModel.Description;

            string specFundingStreamId = _specification.Current.FundingStreams.FirstOrDefault().Id;
            int providerSnapshotId = NewRandomInt();

            CurrentProviderVersionMetadata currentProviderVersionMetadata = new CurrentProviderVersionMetadata
            {
                FundingStreamId = specFundingStreamId,
                ProviderSnapshotId = providerSnapshotId
            };

            AndGetFundingConfiguration(
                specFundingStreamId,
                specificationEditModel.FundingPeriodId,
                withRunCalculationEngineAfterCoreProviderUpdate: withRunCalculationEngineAfterCoreProviderUpdate);

            _providersApiClient.GetCurrentProviderMetadataForFundingStream(specFundingStreamId)
                .Returns(new ApiResponse<CurrentProviderVersionMetadata>(HttpStatusCode.OK, currentProviderVersionMetadata));

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            string correlationId = NewRandomString();

            SpecificationVersion previousSpecificationVersion = _specification.Current;

            //Act
            await service.EditSpecification(SpecificationId, specificationEditModel, user, correlationId);

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

            await _templateVersionChangedHandler
                .Received(1)
                .HandleTemplateVersionChanged(Arg.Is(previousSpecificationVersion),
                    Arg.Any<SpecificationVersion>(),
                    Arg.Is(specificationEditModel.AssignedTemplateIds),
                    Arg.Is(user),
                     Arg.Is(correlationId));

            await _editSpecificationJobActions
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.Name == specificationEditModel.Name),
                    Arg.Any<Reference>(),
                    Arg.Any<string>(),
                    true,
                    withRunCalculationEngineAfterCoreProviderUpdate);
        }

        private SpecificationsService CreateSpecificationsService(Models.Specs.SpecificationVersion newSpecVersion)
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
                mapper: _mapper, 
                logs: _logger, 
                specificationsRepository: _specificationsRepository,
                policiesApiClient: _policiesApiClient, 
                searchRepository: _searchRepository,
                cacheProvider: _cacheProvider, 
                messengerService: _messengerService,
                specificationVersionRepository: _versionRepository,
                providersApiClient: _providersApiClient,
                queueEditSpecificationJobActions: _editSpecificationJobActions);
            return service;
        }

        [DataTestMethod]
        [DataRow("new spec name", "new spec name")]
        [DataRow("   new spec name   ", "new spec name")]
        public async Task EditSpecification_GivenChanges_CreatesNewVersionWithTrimmedName(string specificationEditModelName, string resultSpecificationName)
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = specificationEditModelName,
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name.Trim();
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            _versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);
            AndGetFundingConfiguration(
                _specification.Current.FundingStreams.FirstOrDefault().Id,
                specificationEditModel.FundingPeriodId);

            SpecificationsService service = CreateSpecificationsService(newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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
            SpecificationEditModel specificationEditModel = new SpecificationEditModel
            {
                FundingPeriodId = "fp10",
                Name = "new spec name",
                ProviderVersionId = _specification.Current.ProviderVersionId
            };
            
            Models.Specs.SpecificationVersion newSpecVersion = _specification.Current.Clone() as Models.Specs.SpecificationVersion;
            newSpecVersion.Name = specificationEditModel.Name;
            newSpecVersion.FundingPeriod.Id = specificationEditModel.FundingPeriodId;
            newSpecVersion.FundingStreams = new[] { new Reference { Id = "fs11" } };
            _versionRepository
                .CreateVersion(Arg.Any<Models.Specs.SpecificationVersion>(), Arg.Any<Models.Specs.SpecificationVersion>())
                .Returns(newSpecVersion);

            AndGetFundingConfiguration(
                            _specification.Current.FundingStreams.FirstOrDefault().Id,
                            specificationEditModel.FundingPeriodId);

            var service = CreateSpecificationsService(newSpecVersion);

            //Act
            IActionResult result = await service.EditSpecification(SpecificationId, specificationEditModel, null, null);

            //Assert
            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => ReferenceEquals(_.Current, newSpecVersion)));

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

        private void AndGetFundingConfiguration(
            string fundingStreamId,
            string fundingPeriodId,
            ProviderSource providerSource = ProviderSource.CFS,
            bool withRunCalculationEngineAfterCoreProviderUpdate = false)
        {
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithDefaultTemplateVersion(NewRandomString())
                .WithProviderSource(providerSource)
                .WithRunCalculationEngineAfterCoreProviderUpdate(withRunCalculationEngineAfterCoreProviderUpdate));

            ApiResponse<FundingConfiguration> fundingConfigResponse =
                new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            _policiesApiClient
                .GetFundingConfiguration(
                    Arg.Is(fundingStreamId),
                    Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);
        }
    }
}
