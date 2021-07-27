using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Common.ApiClient.Policies.Models;
using Polly;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DefinitionSpecificationRelationshipServiceTests
    {
        private IDateTimeProvider _dateTimeProvider;
        private DateTime _utcNow;
        private ITypeIdentifierGenerator _typeIdentifierGenerator;

        [TestInitialize]
        public void SetUp()
        {
            _dateTimeProvider = Substitute.For<IDateTimeProvider>();
            _utcNow = NewRandomDateTime().DateTime.ToUniversalTime();
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();

            _dateTimeProvider.UtcNow
                .Returns(_utcNow);
        }

        [TestMethod]
        public async Task CreateRelationship_GivenNullModelProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.CreateRelationship(null, null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null CreateDefinitionSpecificationRelationshipModel was provided to CreateRelationship"));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenModelButWasInvalid_ReturnsBadRequest()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel();

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateDefinitionSpecificationRelationshipModel> validator = CreateRelationshipModelValidator(validationResult);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, relationshipModelValidator: validator);

            //Act
            IActionResult result = await service.CreateRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelButDefinitionCouldNotBeFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns((Models.Datasets.Schema.DatasetDefinition)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.CreateRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"Dataset definition was not found for id {model.DatasetDefinitionId}"));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelButSpecificationCouldNotBeFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition();

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null, null));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.CreateRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
              .Received(1)
              .Error(Arg.Is($"Specification was not found for id {model.SpecificationId}"));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelButFailedToSave_ReturnsFailedResult()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.BadRequest);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.CreateRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(400);

            logger
              .Received(1)
              .Error(Arg.Is($"Failed to save relationship with status code: BadRequest"));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelAndSavesWithoutError_ReturnsOK()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();
            string description = NewRandomString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = datasetDefinitionId
            };

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId
            };

            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId,
                Name = relationshipName,
                Description = description
            };

            Reference author = NewReference();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithDatasetDefinition(NewReference(d => d.WithId(datasetDefinitionId)))
                                                                                          .WithName(relationshipName)
                                                                                          .WithDescription(description)
                                                                                          .WithAuthor(author)
                                                                                          .WithLastUpdated(_utcNow))));

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ICalculationsApiClient calcsClient = Substitute.For<ICalculationsApiClient>();
            calcsClient
                .UpdateBuildProjectRelationships(Arg.Is(specificationId), Arg.Any<DatasetRelationshipSummary>())
                .Returns(new ApiResponse<BuildProject>(HttpStatusCode.OK, new BuildProject
                {
                    Build = new Build
                    {
                        Assembly = new byte[] { },
                        CompilerMessages = new List<CompilerMessage> { new CompilerMessage() },
                        SourceFiles = new List<SourceFile> { new SourceFile() },
                        Success = true
                    },
                    DatasetRelationships = new List<DatasetRelationshipSummary>(1),
                    SpecificationId = "SpecificationId",
                    Id = "Id",
                    Name = "Name"
                }));

            ICalcsRepository calcsRepository = new CalcsRepository(calcsClient,
                new DatasetsResiliencePolicies {CalculationsApiClient = Policy.NoOpAsync()},
                CreateMapper());

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Name == relationshipName), null, null, false)
                .Returns(relationship.Current);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, cacheProvider: cacheProvider,
                calcsRepository: calcsRepository, relationshipVersionRepository: relationshipVersionRepository);



            //Act
            IActionResult result = await service.CreateRelationship(model, author, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                datasetRepository
                    .Received(1)
                    .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(
                        m => !string.IsNullOrWhiteSpace(m.Id) &&
                        m.Current.Description == description &&
                        m.Current.Name == relationshipName &&
                        m.Current.Specification.Id == specificationId &&
                        m.Current.DatasetDefinition.Id == datasetDefinitionId &&
                        m.Current.LastUpdated == _utcNow &&
                        m.Current.Author.Id == author.Id));

            await
              cacheProvider
                  .Received(1)
                  .RemoveAsync<IEnumerable<DatasetSchemaRelationshipModel>>(Arg.Is($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specificationId}"));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelWithPublishedSpecificationConfigurationDetailsShouldSaveWithoutError_ReturnsOK()
        {
            //Arrange
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();
            string description = NewRandomString();
            string targetSpecificationId = NewRandomString();
            string targetFundingStreamId = NewRandomString();
            string targetFundingPeriodId = NewRandomString();
            uint fundingLineIdOne = NewRandomUint();
            uint fundingLineIdOneDup = fundingLineIdOne;
            uint fundingLineIdTwo = NewRandomUint();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdOneDup = calculationIdOne;
            uint calculationIdTwo = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            string templateId = NewRandomString();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId
            };

            SpecModel.SpecificationSummary targetSpecification = new SpecModel.SpecificationSummary
            {
                Id = targetSpecificationId,
                FundingStreams = new[] { new Reference(targetFundingStreamId, targetFundingStreamId) },
                FundingPeriod = new Reference(targetFundingPeriodId, targetFundingPeriodId),
                TemplateIds = new Dictionary<string, string>() { { targetFundingStreamId, templateId } }
            };
            TemplateMetadataDistinctContents metadataContents = new TemplateMetadataDistinctContents()
            {
                FundingLines = new[]
                {
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineOne, TemplateLineId = fundingLineIdOne, Name = fundingLineOne},
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineTwo, TemplateLineId = fundingLineIdTwo, Name = fundingLineTwo}
                },
                Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number},
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdTwo, Name = calculationTwo, Type = Common.TemplateMetadata.Enums.CalculationType.Enum}
                }
            };
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                SpecificationId = specificationId,
                Name = relationshipName,
                Description = description,
                TargetSpecificationId = targetSpecificationId,
                RelationshipType = DatasetRelationshipType.ReleasedData,
                FundingLineIds = new[] { fundingLineIdOne, fundingLineIdOneDup, fundingLineIdTwo },
                CalculationIds = new[] { calculationIdOne, calculationIdOneDup, calculationIdTwo }
            };

            Reference author = NewReference();
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = targetSpecificationId,
                FundingStreamId = targetFundingStreamId,
                FundingPeriodId = targetFundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)},
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdTwo, Name = fundingLineTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTwo)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                    new PublishedSpecificationItem() { TemplateId = calculationIdTwo, Name = calculationTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTwo), FieldType = FieldType.String}
                }
            };
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithName(relationshipName)
                                                                                          .WithDescription(description)
                                                                                          .WithAuthor(author)
                                                                                          .WithLastUpdated(_utcNow)
                                                                                          .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                                                                                          .WithPublishedSpecificationConfiguration(publishedSpecificationConfiguration))));

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();

            ICalculationsApiClient calcsClient = Substitute.For<ICalculationsApiClient>();
            calcsClient
                .UpdateBuildProjectRelationships(Arg.Is(specificationId), Arg.Any<DatasetRelationshipSummary>())
                .Returns(new ApiResponse<BuildProject>(HttpStatusCode.OK, new BuildProject
                {
                    Build = new Build
                    {
                        Assembly = new byte[] { },
                        CompilerMessages = new List<CompilerMessage> { new CompilerMessage() },
                        SourceFiles = new List<SourceFile> { new SourceFile() },
                        Success = true
                    },
                    DatasetRelationships = new List<DatasetRelationshipSummary>(1),
                    SpecificationId = "SpecificationId",
                    Id = "Id",
                    Name = "Name"
                }));

            ICalcsRepository calcsRepository = new CalcsRepository(calcsClient,
                new DatasetsResiliencePolicies { CalculationsApiClient = Policy.NoOpAsync() },
                CreateMapper());

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(targetSpecificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, targetSpecification));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Name == relationshipName))
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Name == relationshipName), null, null, false)
                .Returns(relationship.Current);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(targetFundingStreamId), Arg.Is(targetFundingPeriodId), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, metadataContents, null));

            IJobManagement jobManagement = CreateJobManagement();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, cacheProvider: cacheProvider,
                calcsRepository: calcsRepository, relationshipVersionRepository: relationshipVersionRepository,
                policiesApiClient: policiesApiClient, jobManagement: jobManagement);

            //Act
            IActionResult result = await service.CreateRelationship(model, author, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                datasetRepository
                    .Received(1)
                    .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(
                        m => !string.IsNullOrWhiteSpace(m.Id) &&
                        m.Current.Description == description &&
                        m.Current.Name == relationshipName &&
                        m.Current.Specification.Id == specificationId &&
                        m.Current.LastUpdated == _utcNow &&
                        m.Current.Author.Id == author.Id &&
                        m.Current.PublishedSpecificationConfiguration.SpecificationId == targetSpecificationId &&
                        m.Current.PublishedSpecificationConfiguration.FundingStreamId == targetFundingStreamId &&
                        m.Current.PublishedSpecificationConfiguration.FundingPeriodId == targetFundingPeriodId &&
                        m.Current.PublishedSpecificationConfiguration.FundingLines.Count() == 2 &&
                        m.Current.PublishedSpecificationConfiguration.Calculations.Count() == 2));

            await
              cacheProvider
                  .Received(1)
                  .RemoveAsync<IEnumerable<DatasetSchemaRelationshipModel>>(Arg.Is($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specificationId}"));

           await relationshipVersionRepository
                .Received(1)
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(
                    x => x.Name == relationshipName &&
                    x.PublishedSpecificationConfiguration.FundingLines.Count() == 2 &&
                    x.PublishedSpecificationConfiguration.Calculations.Count() == 2), null, null, false);

            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(_ => _.JobDefinitionId == JobConstants.DefinitionNames.PublishDatasetsDataJob &&
                    _.SpecificationId == specificationId));

            await datasetRepository
                .Received(1)
                .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Name == relationshipName));

            await relationshipVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Name == relationshipName));
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelWithPublishedSpecificationConfigurationDetailsWhenFundingLineNotFound_ThrowsException()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();
            string description = NewRandomString();
            string targetSpecificationId = NewRandomString();
            string targetFundingStreamId = NewRandomString();
            string targetFundingPeriodId = NewRandomString();
            uint fundingLineIdOne = NewRandomUint();
            uint fundingLineIdOneDup = fundingLineIdOne;
            uint fundingLineIdTwo = NewRandomUint();
            uint fundingLineIdUnknown = NewRandomUint();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdOneDup = calculationIdOne;
            uint calculationIdTwo = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            string templateId = NewRandomString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = datasetDefinitionId
            };

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId
            };

            SpecModel.SpecificationSummary targetSpecification = new SpecModel.SpecificationSummary
            {
                Id = targetSpecificationId,
                FundingStreams = new[] { new Reference(targetFundingStreamId, targetFundingStreamId) },
                FundingPeriod = new Reference(targetFundingPeriodId, targetFundingPeriodId),
                TemplateIds = new Dictionary<string, string>() { { targetFundingStreamId, templateId } }
            };
            TemplateMetadataDistinctContents metadataContents = new TemplateMetadataDistinctContents()
            {
                FundingLines = new[]
                {
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineOne, TemplateLineId = fundingLineIdOne, Name = fundingLineOne},
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineTwo, TemplateLineId = fundingLineIdTwo, Name = fundingLineTwo}
                },
                Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number},
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdTwo, Name = calculationTwo, Type = Common.TemplateMetadata.Enums.CalculationType.Enum}
                },
                FundingStreamId = targetFundingStreamId,
                FundingPeriodId = targetFundingPeriodId
            };
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId,
                Name = relationshipName,
                Description = description,
                TargetSpecificationId = targetSpecificationId,
                RelationshipType = DatasetRelationshipType.ReleasedData,
                FundingLineIds = new[] { fundingLineIdOne, fundingLineIdOneDup, fundingLineIdTwo, fundingLineIdUnknown },
                CalculationIds = new[] { calculationIdOne, calculationIdOneDup, calculationIdTwo }
            };

            Reference author = NewReference();
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = targetSpecificationId,
                FundingStreamId = targetFundingStreamId,
                FundingPeriodId = targetFundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)},
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdTwo, Name = fundingLineTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTwo)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                    new PublishedSpecificationItem() { TemplateId = calculationIdTwo, Name = calculationTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTwo), FieldType = FieldType.String}
                }
            };
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithDatasetDefinition(NewReference(d => d.WithId(datasetDefinitionId)))
                                                                                          .WithName(relationshipName)
                                                                                          .WithDescription(description)
                                                                                          .WithAuthor(author)
                                                                                          .WithLastUpdated(_utcNow)
                                                                                          .WithPublishedSpecificationConfiguration(publishedSpecificationConfiguration))));

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ICalculationsApiClient calcsClient = Substitute.For<ICalculationsApiClient>();
            calcsClient
                .UpdateBuildProjectRelationships(Arg.Is(specificationId), Arg.Any<DatasetRelationshipSummary>())
                .Returns(new ApiResponse<BuildProject>(HttpStatusCode.OK, new BuildProject
                {
                    Build = new Build
                    {
                        Assembly = new byte[] { },
                        CompilerMessages = new List<CompilerMessage> { new CompilerMessage() },
                        SourceFiles = new List<SourceFile> { new SourceFile() },
                        Success = true
                    },
                    DatasetRelationships = new List<DatasetRelationshipSummary>(1),
                    SpecificationId = "SpecificationId",
                    Id = "Id",
                    Name = "Name"
                }));

            ICalcsRepository calcsRepository = new CalcsRepository(calcsClient,
                new DatasetsResiliencePolicies { CalculationsApiClient = Policy.NoOpAsync() },
                CreateMapper());

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(targetSpecificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, targetSpecification));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Name == relationshipName))
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Name == relationshipName), null, null, false)
                .Returns(relationship.Current);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(targetFundingStreamId), Arg.Is(targetFundingPeriodId), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, metadataContents, null));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, cacheProvider: cacheProvider,
                calcsRepository: calcsRepository, relationshipVersionRepository: relationshipVersionRepository,
                policiesApiClient: policiesApiClient);

            //Act
            Func<Task<IActionResult>> result = async () => await service.CreateRelationship(model, author, null);

            //Assert
            await result
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage($"No fundingline id '{fundingLineIdUnknown}' in the metadata for FundingStreamId={targetFundingStreamId}, FundingPeriodId={targetFundingPeriodId} and TemplateId={templateId}.");
        }

        [TestMethod]
        public async Task CreateRelationship_GivenValidModelWithPublishedSpecificationConfigurationDetailsWhenCalculationNotFound_ThrowsException()
        {
            //Arrange
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();
            string description = NewRandomString();
            string targetSpecificationId = NewRandomString();
            string targetFundingStreamId = NewRandomString();
            string targetFundingPeriodId = NewRandomString();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdUnknown = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            string templateId = NewRandomString();

            Models.Datasets.Schema.DatasetDefinition definition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = datasetDefinitionId
            };

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId
            };

            SpecModel.SpecificationSummary targetSpecification = new SpecModel.SpecificationSummary
            {
                Id = targetSpecificationId,
                FundingStreams = new[] { new Reference(targetFundingStreamId, targetFundingStreamId) },
                FundingPeriod = new Reference(targetFundingPeriodId, targetFundingPeriodId),
                TemplateIds = new Dictionary<string, string>() { { targetFundingStreamId, templateId } }
            };
            TemplateMetadataDistinctContents metadataContents = new TemplateMetadataDistinctContents()
            {
                Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number},
                },
                FundingStreamId = targetFundingStreamId,
                FundingPeriodId = targetFundingPeriodId
            };
            CreateDefinitionSpecificationRelationshipModel model = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = datasetDefinitionId,
                SpecificationId = specificationId,
                Name = relationshipName,
                Description = description,
                TargetSpecificationId = targetSpecificationId,
                RelationshipType = DatasetRelationshipType.ReleasedData,
                CalculationIds = new[] { calculationIdOne, calculationIdUnknown }
            };

            Reference author = NewReference();
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = targetSpecificationId,
                FundingStreamId = targetFundingStreamId,
                FundingPeriodId = targetFundingPeriodId,
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                }
            };
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithDatasetDefinition(NewReference(d => d.WithId(datasetDefinitionId)))
                                                                                          .WithName(relationshipName)
                                                                                          .WithDescription(description)
                                                                                          .WithAuthor(author)
                                                                                          .WithLastUpdated(_utcNow)
                                                                                          .WithPublishedSpecificationConfiguration(publishedSpecificationConfiguration))));

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetDefinition(Arg.Is(datasetDefinitionId))
                .Returns(definition);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(targetSpecificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, targetSpecification));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Name == relationshipName))
                .Returns(HttpStatusCode.Created);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(targetFundingStreamId), Arg.Is(targetFundingPeriodId), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, metadataContents, null));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, cacheProvider: cacheProvider,
                policiesApiClient: policiesApiClient);

            //Act
            Func<Task<IActionResult>> result = async () => await service.CreateRelationship(model, author, null);

            //Assert
            await result
                .Should()
                .ThrowAsync<NonRetriableException>()
                .WithMessage($"No calculation id '{calculationIdUnknown}' in the metadata for FundingStreamId={targetFundingStreamId}, FundingPeriodId={targetFundingPeriodId} and TemplateId={templateId}.");
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationIdResult(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetRelationshipsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenNoDataReturned_ReturnsOK()
        {
            //Arrange
            string specificationId = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationIdResult(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> items = objectResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            items
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenItemsReturned_ReturnsOK()
        {
            //Arrange
            string specificationId = NewRandomString();

            ILogger logger = CreateLogger();

            int existingMappedVersion = 1;
            int latestVersion = 2;

            string datasetId1 = NewRandomString();
            string datasetId2 = NewRandomString();
            string datasetId3 = NewRandomString();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                NewDefinitionSpecificationRelationship(r => r.WithCurrent(
                                                NewDefinitionSpecificationRelationshipVersion(_=>
                                                            _.WithDatasetVersion(NewDatasetRelationshipVersion(dsrv => dsrv.WithVersion(existingMappedVersion).WithId(datasetId1)))))),
                NewDefinitionSpecificationRelationship(r => r.WithCurrent(
                                                NewDefinitionSpecificationRelationshipVersion(_=>
                                                            _.WithDatasetVersion(NewDatasetRelationshipVersion(dsrv => dsrv.WithVersion(existingMappedVersion).WithId(datasetId2)))))),
                NewDefinitionSpecificationRelationship(r => r.WithCurrent(
                                                NewDefinitionSpecificationRelationshipVersion(_=>
                                                            _.WithDatasetVersion(NewDatasetRelationshipVersion(dsrv => dsrv.WithVersion(existingMappedVersion).WithId(datasetId3)))))),
            };

            Dataset dataset1 = NewDataset(_ => _.WithId(datasetId1));
            Dataset dataset2 = NewDataset(_ => _.WithId(datasetId2));

            KeyValuePair<string, int> keyValuePair1 = new KeyValuePair<string, int>(datasetId1, existingMappedVersion);
            KeyValuePair<string, int> keyValuePair2 = new KeyValuePair<string, int>(datasetId2, latestVersion);

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions = new List<KeyValuePair<string, int>>
            {
                keyValuePair1,
                keyValuePair2,
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .GetDatasetLatestVersions(Arg.Is<IEnumerable<string>>(_ => _.Contains(datasetId1) && _.Contains(datasetId2)))
                .Returns(datasetLatestVersions);
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId1))
                .Returns(dataset1);
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId2))
                .Returns(dataset2);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetRelationshipsBySpecificationIdResult(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> items = objectResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            items
                .Count()
                .Should()
                .Be(3);

            items
                .SingleOrDefault(_ => _.DatasetId == datasetId1)
                .Should()
                .NotBeNull();

            items
                .SingleOrDefault(_ => _.DatasetId == datasetId1)
                .IsLatestVersion
                .Should()
                .BeTrue();

            items
                .SingleOrDefault(_ => _.DatasetId == datasetId2)
                .Should()
                .NotBeNull();

            items
                .SingleOrDefault(_ => _.DatasetId == datasetId2)
                .IsLatestVersion
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task GetRelationshipBySpecificationIdAndName_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipBySpecificationIdAndName(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("The specification id was not provided to GetRelationshipsBySpecificationIdAndName"));
        }

        [TestMethod]
        public async Task GetRelationshipBySpecificationIdAndName_GivenNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            string specificationId = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipBySpecificationIdAndName(specificationId, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("The name was not provided to GetRelationshipsBySpecificationIdAndName"));
        }

        [TestMethod]
        public async Task GetRelationshipBySpecificationIdAndName_GivenRelationshipDoesNotExist_ReturnsNotfound()
        {
            //Arrange
            string specificationId = NewRandomString();
            string name = "test name";

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetRelationshipBySpecificationIdAndName(specificationId, name);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetRelationshipBySpecificationIdAndName_GivenRelationshipFound_ReturnsOKResult()
        {
            //Arrange
            string specificationId = NewRandomString();
            string name = "test name";

            DefinitionSpecificationRelationship relationship = new DefinitionSpecificationRelationship();

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetRelationshipBySpecificationIdAndName(Arg.Is(specificationId), Arg.Is(name))
                .Returns(relationship);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetRelationshipBySpecificationIdAndName(specificationId, name);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenNullSpecificationId_ReturnsBadRequest()
        {
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetCurrentRelationshipsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenSpecificationNotFound_ReturnsPreConditionFailed()
        {
            string specificationId = NewRandomString();

            ILogger logger = CreateLogger();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null, null));


            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to find specification for id: {specificationId}"));
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenNoRelationshipsFound_ReturnsOkAndEmptyList()
        {
            string specificationId = NewRandomString();

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IEnumerable<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                specificationsApiClient: specificationsApiClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            List<DatasetSpecificationRelationshipViewModel> content = okResult.Value as List<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .Any()
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenRelationshipsButDatasetVersionIsNull_ReturnsOkAndList()
        {
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            const string relationshipName = "rel name";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IList<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>()
            {
                NewDefinitionSpecificationRelationship(r =>
                        r.WithId(relationshipId)
                        .WithName(relationshipName)
                        .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                    .WithRelationshipId(relationshipId)
                                                    .WithName(relationshipName))))
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                specificationsApiClient: specificationsApiClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> content = okResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .Any()
                .Should()
                .BeTrue();

            content
               .Count()
               .Should()
               .Be(1);
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenRelationshipsButDatasetVersionIsNullButHasDefinition_ReturnsOkAndList()
        {
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            string definitionId = NewRandomString();
            const string relationshipName = "rel name";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            Models.Datasets.Schema.DatasetDefinition datasetDefinition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = definitionId,
                Name = "def name",
                Description = "def desc"
            };

            IList<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>()
            {
                NewDefinitionSpecificationRelationship(r =>
                        r.WithId(relationshipId)
                        .WithName(relationshipName)
                        .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                    .WithRelationshipId(relationshipId)
                                                    .WithName(relationshipName)
                                                    .WithDatasetDefinition(NewReference(s => s.WithId(definitionId)))
                                                    .WithIsSetAsProviderData(true))))
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .GetDatasetDefinition(Arg.Is(definitionId))
                .Returns(datasetDefinition);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                specificationsApiClient: specificationsApiClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> content = okResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .First()
                .Definition.Name
                .Should()
                .Be("def name");

            content
                .First()
                .Definition.Id
                .Should()
                .Be(definitionId);

            content
                .First()
                .Definition.Description
                .Should()
                .Be("def desc");

            content
                .First()
                .Id
                .Should()
                .Be(relationshipId);

            content
                .First()
                .IsProviderData
                .Should()
                .BeTrue();

            content
               .First()
               .Name
               .Should()
               .Be(relationshipName);
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenRelationshipsWithDatasetVersionButVersionCouldNotBeFound_ReturnsOkAndList()
        {
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            const string relationshipName = "rel name";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            Models.Datasets.Schema.DatasetDefinition datasetDefinition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = definitionId,
                Name = "def name",
                Description = "def desc"
            };

            IList<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>()
            {
                NewDefinitionSpecificationRelationship(r =>
                        r.WithId(relationshipId)
                        .WithName(relationshipName)
                        .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                    .WithRelationshipId(relationshipId)
                                                    .WithName(relationshipName)
                                                    .WithDatasetDefinition(NewReference(s => s.WithId(definitionId)))
                                                    .WithDatasetVersion(NewDatasetRelationshipVersion(d => d.WithId(datasetId).WithVersion(1)))
                                                    .WithIsSetAsProviderData(true))))
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .GetDatasetDefinition(Arg.Is(definitionId))
                .Returns(datasetDefinition);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                specificationsApiClient: specificationsApiClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> content = okResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .First()
                .Definition.Name
                .Should()
                .Be("def name");

            content
                .First()
                .Definition.Id
                .Should()
                .Be(definitionId);

            content
                .First()
                .Definition.Description
                .Should()
                .Be("def desc");

            content
                .First()
                .Id
                .Should()
                .Be(relationshipId);

            content
               .First()
               .Name
               .Should()
               .Be(relationshipName);

            content
                .First()
                .IsProviderData
                .Should()
                .BeTrue();

            logger
                .Received(1)
                .Warning($"Dataset could not be found for Id {datasetId}");
        }

        [TestMethod]
        public async Task GetRelationshipsBySpecificationId_GivenRelationships_ReturnsOkAndList()
        {
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            const string relationshipName = "rel name";
            const string relationshipDescription = "dataset description";

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            Models.Datasets.Schema.DatasetDefinition datasetDefinition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = definitionId,
                Name = "def name",
                Description = "def desc"
            };

            IList<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>()
             {
                NewDefinitionSpecificationRelationship(r =>
                        r.WithId(relationshipId)
                        .WithName(relationshipName)
                        .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                    .WithRelationshipId(relationshipId)
                                                    .WithName(relationshipName)
                                                    .WithDescription(relationshipDescription)
                                                    .WithDatasetDefinition(NewReference(s => s.WithId(definitionId)))
                                                    .WithDatasetVersion(NewDatasetRelationshipVersion(d => d.WithId(datasetId).WithVersion(1)))
                                                    .WithIsSetAsProviderData(true))))
            };

            Dataset dataset = new Dataset
            {
                Id = datasetId,
                Name = "ds name"
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .GetDatasetDefinition(Arg.Is(definitionId))
                .Returns(datasetDefinition);
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                specificationsApiClient: specificationsApiClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> content = okResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .First()
                .Definition.Name
                .Should()
                .Be("def name");

            content
                .First()
                .Definition.Id
                .Should()
                .Be(definitionId);

            content
                .First()
                .Definition.Description
                .Should()
                .Be("def desc");

            content
                .First()
                .Id
                .Should()
                .Be(relationshipId);

            content
               .First()
               .Name
               .Should()
               .Be(relationshipName);

            content
                .First()
                .DatasetId
                .Should()
                .Be(datasetId);

            content
                .First()
                .DatasetName
                .Should()
                .Be("ds name");

            content
                .First()
                .Version
                .Should()
                .Be(1);

            content
                .First()
                .IsProviderData
                .Should()
                .BeTrue();

            content
               .First()
               .RelationshipDescription
               .Should()
               .Be(relationshipDescription);
        }

        [TestMethod]
        public async Task GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId_GivenNoRelationshipsFound_ReturnsOkAndEmptyList()
        {
            string specificationId = NewRandomString();

            string datasetDefinitionId = "12345";

            ILogger logger = CreateLogger();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(specificationId, datasetDefinitionId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            List<DatasetSpecificationRelationshipViewModel> content = okResult.Value as List<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .Any()
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId_GivenRelationships_ReturnsOkAndList()
        {
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            const int exisingDatasetReulationshipVersion = 1;
            const int latestDatasetReulationshipVersion = 2;
            string relationshipName = NewRandomString();
            string relationshipDescription = NewRandomString();

            ILogger logger = CreateLogger();

            Models.Datasets.Schema.DatasetDefinition datasetDefinition = new Models.Datasets.Schema.DatasetDefinition
            {
                Id = definitionId,
                Name = "def name",
                Description = "def desc"
            };

            IList<DefinitionSpecificationRelationship> relationships = new List<DefinitionSpecificationRelationship>()
            {
                NewDefinitionSpecificationRelationship(r =>
                        r.WithId(relationshipId)
                        .WithName(relationshipName)
                        .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                    .WithRelationshipId(relationshipId)
                                                    .WithName(relationshipName)
                                                    .WithDescription(relationshipDescription)
                                                    .WithDatasetDefinition(NewReference(s => s.WithId(definitionId)))
                                                    .WithDatasetVersion(NewDatasetRelationshipVersion(d => d.WithId(datasetId).WithVersion(exisingDatasetReulationshipVersion)))
                                                    .WithIsSetAsProviderData(true))))
            };

            Dataset dataset = NewDataset(_ => _.WithId(datasetId).WithName("ds name"));

            KeyValuePair<string, int> keyValuePair1 = new KeyValuePair<string, int>(datasetId, latestDatasetReulationshipVersion);

            IEnumerable<KeyValuePair<string, int>> datasetLatestVersions = new List<KeyValuePair<string, int>>
            {
                keyValuePair1,
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .GetDatasetDefinition(Arg.Is(definitionId))
                .Returns(datasetDefinition);
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDatasetLatestVersions(Arg.Is<IEnumerable<string>>(_ => _.Contains(datasetId)))
                .Returns(datasetLatestVersions);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(specificationId, definitionId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetSpecificationRelationshipViewModel> content = okResult.Value as IEnumerable<DatasetSpecificationRelationshipViewModel>;

            content
                 .Should()
                 .NotBeNull();

            content
                .First()
                .Definition.Name
                .Should()
                .Be("def name");

            content
                .First()
                .Definition.Id
                .Should()
                .Be(definitionId);

            content
                .First()
                .Definition.Description
                .Should()
                .Be("def desc");

            content
                .First()
                .Id
                .Should()
                .Be(relationshipId);

            content
               .First()
               .Name
               .Should()
               .Be(relationshipName);

            content
                .First()
                .DatasetId
                .Should()
                .Be(datasetId);

            content
                .First()
                .DatasetName
                .Should()
                .Be("ds name");

            content
                .First()
                .Version
                .Should()
                .Be(1);

            content
                .First()
                .IsProviderData
                .Should()
                .BeTrue();

            content
               .First()
               .RelationshipDescription
               .Should()
               .Be(relationshipDescription);

            content
                .First()
                .IsLatestVersion
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task GetDataSourcesByRelationshipId_GivenNullRelationshipIdProvided_ReturnesBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.GetDataSourcesByRelationshipId(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("The relationshipId id was not provided to GetDataSourcesByRelationshipId"));
        }

        [TestMethod]
        public async Task GetDataSourcesByRelationshipId_GivenRelationshipNotFound_ReturnsPreConditionFailed()
        {
            string relationshipId = NewRandomString();

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns((DefinitionSpecificationRelationship)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetDataSourcesByRelationshipId(relationshipId);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);
        }

        [TestMethod]
        public async Task GetDataSourcesByRelationshipId_GivenRelationshipFoundButNoDatasets_ReturnsOKResult()
        {
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(NewRandomString())))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithDatasetDefinition(NewReference(s => s.WithId(NewRandomString()))))));
            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetDataSourcesByRelationshipId(relationshipId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task ToggleDatasetRelationship_GivenConverterEnabledOnDefintion_ReturnsOk()
        {
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(NewRandomString())))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithDatasetDefinition(NewReference(s => s.WithId(NewRandomString()))))));
            IDatasetRepository datasetRepository = CreateDatasetRepository();

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);

            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId))
                .Returns(HttpStatusCode.OK);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.ToggleDatasetRelationship(relationshipId, true);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(200);
        }

        [TestMethod]
        public async Task GetDataSourcesByRelationshipId_GivenReleasedRelationshipFoundAndDatasetFound_ReturnsOKResult()
        {
            string relationshipId = NewRandomString();
            string datasetComment = NewRandomString();
            DateTimeOffset datasetDate = NewRandomDateTime();
            string datasetAuthorId = NewRandomString();
            string datasetAuthorName = NewRandomString();
            string datasetName = NewRandomString();
            string datasetDescription = NewRandomString();
            string relationshipName = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(NewRandomString())))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                                                                                          .WithName(relationshipName)
                                                                                          .WithDatasetDefinition(NewReference(s => s.WithId(NewRandomString()))))));

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);

            IEnumerable<Dataset> datasets = new[]
            {
                NewDataset(_ =>_
                    .WithId(NewRandomString())
                    .WithName(datasetName)
                    .WithDescription(datasetDescription)
                    .WithHistory(
                        NewDatasetVersion(dv=> dv
                            .WithVersion(1)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            ),
                        NewDatasetVersion(dv=> dv
                            .WithVersion(3)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            ),
                        NewDatasetVersion(dv=> dv
                            .WithVersion(2)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            )
                    ))
            };

            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(datasets);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetDataSourcesByRelationshipId(relationshipId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            SelectDatasourceModel sourceModel = okObjectResult.Value as SelectDatasourceModel;

            sourceModel
                .Datasets
                .Count()
                .Should()
                .Be(1);

            sourceModel
               .Datasets
               .First()
               .Name
               .Should()
               .Be(datasetName);

            sourceModel
               .Datasets
               .First()
               .Description
               .Should()
               .Be(datasetDescription);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Version
                .Should()
                .Be(3);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Comment
                .Should()
                .Be(datasetComment);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Date
                .Should()
                .Be(datasetDate);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Should()
                .NotBeNull();

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Id
                .Should()
                .Be(datasetAuthorId);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Name
                .Should()
                .Be(datasetAuthorName);

            sourceModel
                .Datasets
                .First()
                .SelectedVersion
                .Should()
                .BeNull();
        }


        [TestMethod]
        public async Task GetDataSourcesByRelationshipId_GivenRelationshipFoundAndDatasetsFound_ReturnsOKResult()
        {
            string relationshipId = NewRandomString();
            string datasetComment = NewRandomString();
            DateTimeOffset datasetDate = NewRandomDateTime();
            string datasetAuthorId = NewRandomString();
            string datasetAuthorName = NewRandomString();
            string datasetName = NewRandomString();
            string datasetDescription = NewRandomString();
            string relationshipName = NewRandomString();

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(NewRandomString())))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithDatasetDefinition(NewReference(s => s.WithId(NewRandomString()))))));

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);

            IEnumerable<Dataset> datasets = new[]
            {
                NewDataset(_ =>_
                    .WithId(NewRandomString())
                    .WithName(datasetName)
                    .WithDescription(datasetDescription)
                    .WithHistory(
                        NewDatasetVersion(dv=> dv
                            .WithVersion(1)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            ),
                        NewDatasetVersion(dv=> dv
                            .WithVersion(3)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            ),
                        NewDatasetVersion(dv=> dv
                            .WithVersion(2)
                            .WithComment(datasetComment)
                            .WithDate(datasetDate)
                            .WithAuthor(
                                NewReference(r=>r
                                    .WithId(datasetAuthorId)
                                    .WithName(datasetAuthorName)))
                            )
                    ))
            };

            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(datasets);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.GetDataSourcesByRelationshipId(relationshipId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            SelectDatasourceModel sourceModel = okObjectResult.Value as SelectDatasourceModel;

            sourceModel
                .Datasets
                .Count()
                .Should()
                .Be(1);

            sourceModel
               .Datasets
               .First()
               .Name
               .Should()
               .Be(datasetName);

            sourceModel
               .Datasets
               .First()
               .Description
               .Should()
               .Be(datasetDescription);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Version
                .Should()
                .Be(3);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Comment
                .Should()
                .Be(datasetComment);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Date
                .Should()
                .Be(datasetDate);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Should()
                .NotBeNull();

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Id
                .Should()
                .Be(datasetAuthorId);

            sourceModel
                .Datasets
                .First()
                .Versions
                .First()
                .Author
                .Name
                .Should()
                .Be(datasetAuthorName);

            sourceModel
                .Datasets
                .First()
                .SelectedVersion
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_GivenNullModelProvided_ReturnesBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(null, null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null AssignDatasourceModel was provided to AssignDatasourceVersionToRelationship"));
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_GivenModelDatasetNotFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns((Dataset)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error($"Dataset not found for dataset id: {datasetId}");
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_GivenModelButRelationshipNotFound_ReturnsPreConditionFailed()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId
            };

            ILogger logger = CreateLogger();

            Dataset dataset = new Dataset();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns((DefinitionSpecificationRelationship)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, null, null);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error($"Relationship not found for relationship id: {relationshipId}");
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_GivenModelButSavingReturnsBadRequest_ThrowsAnException()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId,
                Version = 1
            };

            ILogger logger = CreateLogger();

            Dataset dataset = new Dataset();

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName))));

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.BadRequest);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                ProviderSource = ProviderSource.CFS
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient);

            //Act
            Func<Task> test = async () => await service.AssignDatasourceVersionToRelationship(model, null, null);

            //Assert
            test
               .Should()
               .ThrowExactly<RetriableException>();

            logger
                .Received(1)
                .Error($"Failed to save relationship - {relationshipName} with status code: BadRequest");
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_GivenModelAndSaves_ReturnsNoContent()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();
            string specificationId = NewRandomString();
            string jobId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId,
                Version = 1
            };

            ILogger logger = CreateLogger();
            Reference user = NewReference();

            Dataset dataset = new Dataset();
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName))));

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , null
                             , false)
                .Returns(relationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(_ =>
                    ReferenceEquals(_.Current.Author, user) &&
                    _.Current.LastUpdated == _utcNow))
                .Returns(HttpStatusCode.OK);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                ProviderSource = ProviderSource.CFS
            };

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapDatasetJob &&
                                                    j.SpecificationId == relationship.Current.Specification.Id &&
                                                    j.Trigger.EntityId == relationship.Id))
                .Returns(new Job { Id = jobId });

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            DefinitionSpecificationRelationshipService service = CreateService(
                logger: logger,
                datasetRepository: datasetRepository,
                specificationsApiClient: specificationsApiClient,
                jobManagement: jobManagement,
                relationshipVersionRepository: relationshipVersionRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, user, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<JobCreationResponse>();

            JobCreationResponse jobCreationResponse = (result as OkObjectResult).Value as JobCreationResponse;

            jobCreationResponse
                .JobId
                .Should()
                .Be(jobId);

            await relationshipVersionRepository
                 .Received(1)
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , null
                             , false);

            await datasetRepository
                 .Received(1)
                 .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(_ =>
                     ReferenceEquals(_.Current.Author, user) &&
                     _.Current.LastUpdated == _utcNow));

            await relationshipVersionRepository
                 .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_JobServiceFeatureToggleSwitchedOn_CallsJobServiceInsteadOfQueuingDirectly()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();
            string specificationId = NewRandomString();
            string jobId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId,
                Version = 1
            };

            Reference user = new Reference("user-id-1", "user-name-1");

            ILogger logger = CreateLogger();

            Dataset dataset = new Dataset();
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName))));

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , null
                             , false)
                .Returns(relationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.OK);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                ProviderSource = ProviderSource.CFS
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));


            IJobManagement jobManagement = CreateJobManagement();

            jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapDatasetJob &&
                                                    j.SpecificationId == specificationId &&
                                                    j.Trigger.EntityId == relationship.Id))
                .Returns(new Job { Id = jobId });

            IMessengerService messengerService = CreateMessengerService();

            DefinitionSpecificationRelationshipService service = CreateService(
                logger: logger,
                datasetRepository: datasetRepository,
                jobManagement: jobManagement,
                messengerService: messengerService,
                specificationsApiClient: specificationsApiClient,
                relationshipVersionRepository: relationshipVersionRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, user, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<JobCreationResponse>();

            JobCreationResponse jobCreationResponse = (result as OkObjectResult).Value as JobCreationResponse;

            jobCreationResponse
                .JobId
                .Should()
                .Be(jobId);

            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == "MapDatasetJob" &&
                                                       j.SpecificationId == specificationId &&
                                                       j.Properties.ContainsKey("user-id") &&
                                                       j.Properties["user-id"] == user.Id &&
                                                       j.Properties.ContainsKey("user-name") &&
                                                       j.Properties["user-name"] == user.Name));

            await messengerService
                .DidNotReceive()
                .SendToQueue(Arg.Any<string>(), Arg.Any<Dataset>(), Arg.Any<IDictionary<string, string>>());

            await relationshipVersionRepository
                .Received(1)
               .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                            , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                            , null
                            , false);

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId));

            await relationshipVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_ForFDZProviderSource_CreateMapFDZDatasetJob()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();
            string specificationId = NewRandomString();
            string jobId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId,
                Version = 1
            };

            Reference user = new Reference("user-id-1", "user-name-1");

            ILogger logger = CreateLogger();

            Dataset dataset = new Dataset();
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithIsSetAsProviderData(true))));

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , null
                             , false)
                .Returns(relationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.OK);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                ProviderSource = ProviderSource.FDZ
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));


            IJobManagement jobManagement = CreateJobManagement();

            jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapFdzDatasetsJob &&
                                                    j.SpecificationId == specificationId &&
                                                    j.Trigger.EntityId == relationshipId))
                .Returns(new Job { Id = jobId });

            IMessengerService messengerService = CreateMessengerService();

            DefinitionSpecificationRelationshipService service = CreateService(
                logger: logger, datasetRepository: datasetRepository, jobManagement: jobManagement,
                messengerService: messengerService, specificationsApiClient: specificationsApiClient,
                relationshipVersionRepository: relationshipVersionRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, user, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<JobCreationResponse>();

            JobCreationResponse jobCreationResponse = (result as OkObjectResult).Value as JobCreationResponse;

            jobCreationResponse
                .JobId
                .Should()
                .Be(jobId);

            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapFdzDatasetsJob &&
                                                       j.SpecificationId == specificationId &&
                                                       j.Properties.ContainsKey("user-id") &&
                                                       j.Properties["user-id"] == user.Id &&
                                                       j.Properties.ContainsKey("user-name") &&
                                                       j.Properties["user-name"] == user.Name &&
                                                       j.Properties.ContainsKey("relationship-id") &&
                                                       j.Properties["relationship-id"] == relationshipId));

            await messengerService
                .DidNotReceive()
                .SendToQueue(Arg.Any<string>(), Arg.Any<Dataset>(), Arg.Any<IDictionary<string, string>>());

            await relationshipVersionRepository
               .Received(1)
              .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                           , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                           , null
                           , false);

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId));

            await relationshipVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));
        }

        [TestMethod]
        public async Task AssignDatasourceVersionToRelationship_ForScopedProviderDataset_CreateMapScopedDatasetJob()
        {
            //Arrange
            string datasetId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString();
            string specificationId = NewRandomString();
            string jobId = NewRandomString();

            AssignDatasourceModel model = new AssignDatasourceModel
            {
                DatasetId = datasetId,
                RelationshipId = relationshipId,
                Version = 1
            };

            Reference user = new Reference("user-id-1", "user-name-1");

            ILogger logger = CreateLogger();

            Dataset dataset = new Dataset();
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithIsSetAsProviderData(true))));

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                             , null
                             , false)
                .Returns(relationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(dataset);
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(relationship);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.OK);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                ProviderSource = ProviderSource.CFS
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));


            IJobManagement jobManagement = CreateJobManagement();

            jobManagement.QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapScopedDatasetJob &&
                                                       j.SpecificationId == specificationId &&
                                                       j.Properties.ContainsKey("provider-cache-key") &&
                                                       j.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}" &&
                                                       j.Properties.ContainsKey("specification-summary-cache-key") &&
                                                       j.Properties["specification-summary-cache-key"] == $"{CacheKeys.SpecificationSummaryById}{specificationId}"))
                .Returns(new Job { Id = "parentJobId" });

            jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapDatasetJob &&
                                                    j.SpecificationId == specificationId &&
                                                    j.Trigger.EntityId == relationshipId))
                .Returns(new Job { Id = jobId });

            IMessengerService messengerService = CreateMessengerService();

            DefinitionSpecificationRelationshipService service = CreateService(
                logger: logger, datasetRepository: datasetRepository, jobManagement: jobManagement,
                messengerService: messengerService, specificationsApiClient: specificationsApiClient,
                relationshipVersionRepository: relationshipVersionRepository);

            //Act
            IActionResult result = await service.AssignDatasourceVersionToRelationship(model, user, null);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<JobCreationResponse>();

            JobCreationResponse jobCreationResponse = (result as OkObjectResult).Value as JobCreationResponse;

            jobCreationResponse
                .JobId
                .Should()
                .Be(jobId);

            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapDatasetJob &&
                                                       j.SpecificationId == specificationId &&
                                                       j.Properties.ContainsKey("user-id") &&
                                                       j.Properties["user-id"] == user.Id &&
                                                       j.Properties.ContainsKey("user-name") &&
                                                       j.Properties["user-name"] == user.Name &&
                                                       j.Properties.ContainsKey("relationship-id") &&
                                                       j.Properties["relationship-id"] == relationshipId &&
                                                       j.Properties.ContainsKey("parentJobId") &&
                                                       j.Properties["parentJobId"] == "parentJobId"));

            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.MapScopedDatasetJob &&
                                                       j.SpecificationId == specificationId &&
                                                       j.Properties.ContainsKey("provider-cache-key") &&
                                                       j.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}" &&
                                                       j.Properties.ContainsKey("specification-summary-cache-key") &&
                                                       j.Properties["specification-summary-cache-key"] == $"{CacheKeys.SpecificationSummaryById}{specificationId}"));

            await messengerService
                .DidNotReceive()
                .SendToQueue(Arg.Any<string>(), Arg.Any<Dataset>(), Arg.Any<IDictionary<string, string>>());

            await relationshipVersionRepository
               .Received(1)
              .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                           , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId)
                           , null
                           , false);

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId));

            await relationshipVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));
        }

        [TestMethod]
        public async Task GetSpecificationIdsForRelationshipDefinitionId_GivenRelationshipsExist_ReturnsListOfSpecificationIds()
        {
            //Arrange
            const string datasetDefinitionId = "defid-1";

            IEnumerable<string> specIds = Enumerable.Empty<string>();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(Arg.Is(datasetDefinitionId))
                .Returns(specIds);

            DefinitionSpecificationRelationshipService service = CreateService(datasetRepository: datasetRepository);

            //Act
            IActionResult actionResult = await service.GetSpecificationIdsForRelationshipDefinitionId(datasetDefinitionId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            IEnumerable<string> specificationIds = (actionResult as OkObjectResult).Value as IEnumerable<string>;

            specificationIds
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void UpdateRelationshipDatasetDefinitionName_GivenANullDefinitionRefrenceSupplied_LogsAndThrowsException()
        {
            //Arrange
            Reference reference = null;

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            //Act
            Func<Task> test = () => service.UpdateRelationshipDatasetDefinitionName(reference);

            //Assert
            test
               .Should()
               .ThrowExactly<NonRetriableException>();

            logger
                .Received(1)
                .Error("Null dataset definition reference supplied");
        }

        [TestMethod]
        public async Task UpdateRelationshipDatasetDefinitionName_GivenNoRelationshipsFound_LogsAndDoesNotProcess()
        {
            //Arrange
            const string definitionId = "id-1";
            const string definitionName = "name-1";

            Reference reference = new Reference(definitionId, definitionName);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(Enumerable.Empty<DefinitionSpecificationRelationship>());

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            await service.UpdateRelationshipDatasetDefinitionName(reference);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("No relationships found to update"));
        }

        [TestMethod]
        public void UpdateRelationshipDatasetDefinitionName_GivenRelationshipsButExceptionRaisedWhenUpdating_LogsAndThrowsRetriableException()
        {
            //Arrange
            const string definitionId = "id-1";
            const string definitionName = "name-1";

            Reference reference = new Reference(definitionId, definitionName);

            ILogger logger = CreateLogger();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                new DefinitionSpecificationRelationship(),
                new DefinitionSpecificationRelationship()
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);

            datasetRepository
                .When(x => x.UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>()))
                .Do(x => throw new Exception("Failed to update relationship"));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            //Act
            Func<Task> test = () => service.UpdateRelationshipDatasetDefinitionName(reference);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to update relationships with new definition name: {definitionName}");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());

            logger
                .Received(1)
                .Information(Arg.Is($"Updating 2 relationships with new definition name: {definitionName}"));

            logger
                .DidNotReceive()
                .Information($"Updated 2 relationships with new definition name: {definitionName}");
        }

        [TestMethod]
        public async Task UpdateRelationshipDatasetDefinitionName_GivenRelationshipsAndUpdated_LogsSuccess()
        {
            //Arrange
            string definitionId = NewRandomString();
            string oldDefinitionName = NewRandomString();
            string newDefinitionName = NewRandomString();
            string relationshipIdOne = NewRandomString();
            string relationshipIdTwo = NewRandomString();

            Reference reference = new Reference(definitionId, newDefinitionName);

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipVersion relationshipVersionOne = NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithDatasetDefinition(NewReference(s => s.WithId(definitionId).WithName(oldDefinitionName)))
                                                    .WithRelationshipId(relationshipIdOne));

            DefinitionSpecificationRelationshipVersion relationshipVersionTwo = NewDefinitionSpecificationRelationshipVersion(_ =>
                                                    _.WithDatasetDefinition(NewReference(s => s.WithId(definitionId).WithName(oldDefinitionName)))
                                                    .WithRelationshipId(relationshipIdTwo));
            DefinitionSpecificationRelationship relationshipOne = NewDefinitionSpecificationRelationship(r =>
                        r.WithCurrent(relationshipVersionOne).WithId(relationshipIdOne));
            DefinitionSpecificationRelationship relationshipTwo = NewDefinitionSpecificationRelationship(r =>
                        r.WithCurrent(relationshipVersionTwo).WithId(relationshipIdTwo));

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[] { relationshipOne, relationshipTwo };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
            datasetRepository
                .UpdateDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.OK);

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            DefinitionSpecificationRelationshipVersion newRelationshipVersionOne = (DefinitionSpecificationRelationshipVersion)relationshipVersionOne.Clone();
            newRelationshipVersionOne.DatasetDefinition.Name = newDefinitionName;
            relationshipVersionRepository.CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Id == relationshipVersionOne.Id),
                                                        Arg.Any<DefinitionSpecificationRelationshipVersion>(), null, false)
                .Returns(newRelationshipVersionOne);

            DefinitionSpecificationRelationshipVersion newRelationshipVersionTwo = (DefinitionSpecificationRelationshipVersion)relationshipVersionTwo.Clone();
            newRelationshipVersionTwo.DatasetDefinition.Name = newDefinitionName;
            relationshipVersionRepository.CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Id == relationshipVersionTwo.Id),
                                                        Arg.Any<DefinitionSpecificationRelationshipVersion>(), null, false)
                .Returns(newRelationshipVersionTwo);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger
                , datasetRepository: datasetRepository
                , relationshipVersionRepository: relationshipVersionRepository);

            //Act
            await service.UpdateRelationshipDatasetDefinitionName(reference);

            //Assert
            await
                datasetRepository
                    .Received(2)
                    .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(m =>
                            m.Current.DatasetDefinition.Name == newDefinitionName));
            await relationshipVersionRepository
               .Received(1)
              .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne)
                           , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne)
                           , null
                           , false);
            await relationshipVersionRepository
               .Received(1)
              .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo)
                           , Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo)
                           , null
                           , false);

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdOne));
            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdTwo));

            await relationshipVersionRepository
                .Received(1)
                .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne));
            await relationshipVersionRepository
               .Received(1)
               .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo));
        }

        [TestMethod]
        public async Task Migrate_ShouldMigrateFromOldDefinitionSpecificationRelationshipToDefinitionSpecificationRelationship()
        {
            // Arrange
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();
            string relationshipName = NewRandomString();

            OldDefinitionSpecificationRelationship existingRelationship = new OldDefinitionSpecificationRelationship()
            {
                Id = relationshipId,
                Name = relationshipName,
                Content = new OldDefinitionSpecificationRelationshipContent()
                {
                    Specification = NewReference(s => s.WithId(specificationId)),
                    IsSetAsProviderData = true
                }
            };

            DefinitionSpecificationRelationshipVersion relationshipVersion = NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                         _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipId)
                                                                                          .WithName(relationshipName)
                                                                                          .WithIsSetAsProviderData(true));
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipId)
                                                              .WithName(relationshipName)
                                                              .WithCurrent(relationshipVersion));

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository.GetDefinitionSpecificationRelationshipsToMigrate()
                .Returns(new[] { existingRelationship });
            datasetRepository.UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId))
                .Returns(HttpStatusCode.OK);

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();
            relationshipVersionRepository.CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId))
                .Returns(relationshipVersion);

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger
                , datasetRepository: datasetRepository
                , relationshipVersionRepository: relationshipVersionRepository);

            // Act
            IActionResult result = await service.Migrate();

            // Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await datasetRepository
                .Received(1)
                .GetDefinitionSpecificationRelationshipsToMigrate();

            await relationshipVersionRepository
                .Received(1)
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipId));

            await relationshipVersionRepository
               .Received(1)
               .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipId));
        }

        [TestMethod]
        public async Task Migrate_ShouldMigrateMulitpleOldDefinitionSpecificationRelationshipSToDefinitionSpecificationRelationships()
        {
            // Arrange
            string relationshipIdOne = NewRandomString();
            string relationshipIdTwo = NewRandomString();
            string specificationId = NewRandomString();

            OldDefinitionSpecificationRelationship existingRelationshipOne = new OldDefinitionSpecificationRelationship()
            {
                Id = relationshipIdOne,
                Content = new OldDefinitionSpecificationRelationshipContent()
                {
                    Specification = NewReference(s => s.WithId(specificationId)),
                }
            };
            OldDefinitionSpecificationRelationship existingRelationshipTwo = new OldDefinitionSpecificationRelationship()
            {
                Id = relationshipIdTwo,
                Content = new OldDefinitionSpecificationRelationshipContent()
                {
                    Specification = NewReference(s => s.WithId(specificationId)),
                }
            };

            DefinitionSpecificationRelationshipVersion relationshipVersionOne = NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                         _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipIdOne));

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipIdOne)
                                                              .WithCurrent(relationshipVersionOne));

            DefinitionSpecificationRelationshipVersion relationshipVersionTwo = NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                         _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithRelationshipId(relationshipIdTwo));

            DefinitionSpecificationRelationship relationshipTwo = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithId(relationshipIdTwo)
                                                              .WithCurrent(relationshipVersionTwo));

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository.GetDefinitionSpecificationRelationshipsToMigrate()
                .Returns(new[] { existingRelationshipOne, existingRelationshipTwo });
            datasetRepository.UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdOne))
                .Returns(HttpStatusCode.OK);
            datasetRepository.UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdTwo))
                .Returns(HttpStatusCode.OK);

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();
            relationshipVersionRepository.CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne))
                .Returns(relationshipVersionOne);
            relationshipVersionRepository.CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo))
                .Returns(relationshipVersionTwo);

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger
                , datasetRepository: datasetRepository
                , relationshipVersionRepository: relationshipVersionRepository);

            // Act
            IActionResult result = await service.Migrate();

            // Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await datasetRepository
                .Received(1)
                .GetDefinitionSpecificationRelationshipsToMigrate();

            await relationshipVersionRepository
                .Received(1)
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne));
            await relationshipVersionRepository
               .Received(1)
               .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo));

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdOne));

            await datasetRepository
                .Received(1)
                .UpdateDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(x => x.Id == relationshipIdTwo));

            await relationshipVersionRepository
               .Received(1)
               .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdOne));
            await relationshipVersionRepository
               .Received(1)
               .SaveVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.RelationshipId == relationshipIdTwo));
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenNullModelProvided_ReturnsBadRequest()
        {
            ILogger logger = CreateLogger();
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            IActionResult result = await service.UpdateRelationship(null, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null UpdateDefinitionSpecificationRelationshipModel was provided to UpdateRelationship"));
        }

        [TestMethod]
        public void UpdateRelationship_GivenNullRelationshipIdProvided_ReturnsBadRequest()
        {
            string specificationId = NewRandomString();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = "desc",
                FundingLineIds = new List<uint>(),
                CalculationIds = new List<uint>()
            };

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            Func<Task> test = async () => await service.UpdateRelationship(model, specificationId, null);

            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateRelationship_GivenNullSpecificationIdProvided_ReturnsBadRequest()
        {
            string relationshipId = NewRandomString();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = "desc",
                FundingLineIds = new List<uint>(),
                CalculationIds = new List<uint>()
            };

            ILogger logger = CreateLogger();

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger);

            Func<Task> test = async () => await service.UpdateRelationship(model, null, relationshipId);

            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenModelButWasInvalid_ReturnesBadRequest()
        {
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel();

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<UpdateDefinitionSpecificationRelationshipModel> validator = CreateUpdateRelationshipModelValidator(validationResult);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, updateRelationshipModelValidator: validator);

            IActionResult result = await service.UpdateRelationship(model, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenValidModelButDefinitionSpecificationRelationshipCouldNotBeFound_ReturnsNotFound()
        {
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = "desc",
                FundingLineIds = new List<uint>(),
                CalculationIds = new List<uint>()
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns((DefinitionSpecificationRelationship)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            IActionResult result = await service.UpdateRelationship(model, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(404);
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenValidModelButDefinitionSpecificationRelationshipWrongRelationshipType_ReturnsNotFound()
        {
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = "desc",
                FundingLineIds = new List<uint>(),
                CalculationIds = new List<uint>()
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(new DefinitionSpecificationRelationship
                {
                    Current = new DefinitionSpecificationRelationshipVersion
                    {
                        RelationshipType = DatasetRelationshipType.Uploaded
                    }
                });

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            IActionResult result = await service.UpdateRelationship(model, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(404);
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenValidModelButFailedToSave_ReturnsFailedResult()
        {
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string relationshipName = NewRandomString();
            string description = NewRandomString();
            uint fundingLineIdOne = NewRandomUint();
            uint fundingLineIdTwo = NewRandomUint();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdTwo = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            Reference author = NewReference();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = "desc",
                FundingLineIds = new List<uint>() { fundingLineIdOne, fundingLineIdTwo },
                CalculationIds = new List<uint>() { calculationIdOne, calculationIdTwo }
            };

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new[] { new Reference(fundingStreamId, fundingStreamId) },
                FundingPeriod = new Reference(fundingPeriodId, fundingPeriodId),
                TemplateIds = new Dictionary<string, string>() { { fundingStreamId, templateId } }
            };
            TemplateMetadataDistinctContents metadataContents = new TemplateMetadataDistinctContents()
            {
                FundingLines = new[]
                {
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineOne, TemplateLineId = fundingLineIdOne, Name = fundingLineOne},
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineTwo, TemplateLineId = fundingLineIdTwo, Name = fundingLineTwo}
                },
                Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number},
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdTwo, Name = calculationTwo, Type = Common.TemplateMetadata.Enums.CalculationType.Enum}
                }
            };
            PublishedSpecificationConfiguration publishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)},
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdTwo, Name = fundingLineTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTwo)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                    new PublishedSpecificationItem() { TemplateId = calculationIdTwo, Name = calculationTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTwo), FieldType = FieldType.String}
                }
            };
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r =>
                                                              r.WithName(relationshipName)
                                                              .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                                                                                          _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                                                                                          .WithDatasetDefinition(NewReference(d => d.WithId(relationshipId)))
                                                                                          .WithName(relationshipName)
                                                                                          .WithDescription(description)
                                                                                          .WithAuthor(author)
                                                                                          .WithLastUpdated(_utcNow)
                                                                                          .WithPublishedSpecificationConfiguration(publishedSpecificationConfiguration))));

            ILogger logger = CreateLogger();

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Name == relationshipName), null, null, false)
                .Returns(relationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(new DefinitionSpecificationRelationship
                {
                    Current = new DefinitionSpecificationRelationshipVersion
                    {
                        Specification = new Reference
                        {
                            Id = specificationId,
                            Name = specificationId
                        },
                        RelationshipType = DatasetRelationshipType.ReleasedData
                    }
                });

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(fundingStreamId), Arg.Any<string>(), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, metadataContents, null));

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.BadRequest);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, policiesApiClient: policiesApiClient,
                relationshipVersionRepository: relationshipVersionRepository);

            IActionResult result = await service.UpdateRelationship(model, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(400);

            logger
              .Received(1)
              .Error(Arg.Is($"Failed to save relationship with status code: BadRequest"));
        }

        [TestMethod]
        public async Task UpdateRelationship_GivenValidModelAndSavesWithoutError_ReturnsOK()
        {
            string relationshipId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string originalDescription = NewRandomString();
            string newDescription = NewRandomString();
            uint fundingLineIdOne = NewRandomUint();
            uint fundingLineIdTwo = NewRandomUint();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdTwo = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            Reference author = NewReference();

            UpdateDefinitionSpecificationRelationshipModel model = new UpdateDefinitionSpecificationRelationshipModel
            {
                Description = newDescription,
                FundingLineIds = new List<uint>() { fundingLineIdOne, fundingLineIdTwo },
                CalculationIds = new List<uint>() { calculationIdOne }
            };

            PublishedSpecificationConfiguration originalPublishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)},
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdTwo, Name = fundingLineTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTwo)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                    new PublishedSpecificationItem() { TemplateId = calculationIdTwo, Name = calculationTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTwo), FieldType = FieldType.String}
                }
            };

            PublishedSpecificationConfiguration newPublishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)},
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdTwo, Name = fundingLineTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTwo)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal}
                }
            };
            DefinitionSpecificationRelationship newRelationship = NewDefinitionSpecificationRelationship(r =>
                r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ =>
                    _.WithSpecification(NewReference(s => s.WithId(specificationId)))
                    .WithDatasetDefinition(NewReference(d => d.WithId(relationshipId)))
                    .WithDescription(newDescription)
                    .WithAuthor(author)
                    .WithLastUpdated(_utcNow)
                    .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                    .WithPublishedSpecificationConfiguration(newPublishedSpecificationConfiguration))));

            ILogger logger = CreateLogger();

            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = CreateRelationshipVersionRepository();

            relationshipVersionRepository
                .CreateVersion(Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Specification.Id == specificationId), Arg.Is<DefinitionSpecificationRelationshipVersion>(x => x.Specification.Id == specificationId), null, false)
                .Returns(newRelationship.Current);

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(new DefinitionSpecificationRelationship
                {
                    Current = new DefinitionSpecificationRelationshipVersion
                    {
                        Specification = new Reference
                        {
                            Id = specificationId,
                            Name = specificationId
                        },
                        LastUpdated = _utcNow,
                        Description = originalDescription,
                        RelationshipId = relationshipId,
                        RelationshipType = DatasetRelationshipType.ReleasedData,
                        PublishedSpecificationConfiguration = originalPublishedSpecificationConfiguration
                    }
                });

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary
                {
                    Id = specificationId,
                    FundingStreams = new[] { new Reference(fundingStreamId, fundingStreamId) },
                    FundingPeriod = new Reference(fundingPeriodId, fundingPeriodId),
                    TemplateIds = new Dictionary<string, string>() { { fundingStreamId, templateId } }
                }));

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(fundingStreamId), Arg.Any<string>(), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK,
                new TemplateMetadataDistinctContents()
                {
                    FundingLines = new[]
                    {
                        new TemplateMetadataFundingLine() { FundingLineCode = fundingLineOne, TemplateLineId = fundingLineIdOne, Name = fundingLineOne},
                        new TemplateMetadataFundingLine() { FundingLineCode = fundingLineTwo, TemplateLineId = fundingLineIdTwo, Name = fundingLineTwo}
                    },
                    Calculations = new[]
                    {
                        new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number},
                        new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdTwo, Name = calculationTwo, Type = Common.TemplateMetadata.Enums.CalculationType.Enum}
                    }
                }, null));

            ICalcsRepository calcsRepository = CreateCalcsRepository();

            calcsRepository.ReMapSpecificationReference(Arg.Is(specificationId), Arg.Is(relationshipId))
                .Returns(new Job { JobDefinitionId = JobConstants.DefinitionNames.ReferencedSpecificationReMapJob });

            datasetRepository
                .SaveDefinitionSpecificationRelationship(Arg.Any<DefinitionSpecificationRelationship>())
                .Returns(HttpStatusCode.OK);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger,
                datasetRepository: datasetRepository, specificationsApiClient: specificationsApiClient, policiesApiClient: policiesApiClient,
                relationshipVersionRepository: relationshipVersionRepository, calcsRepository: calcsRepository);

            IActionResult result = await service.UpdateRelationship(model, specificationId, relationshipId);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                datasetRepository
                    .Received(1)
                    .SaveDefinitionSpecificationRelationship(Arg.Is<DefinitionSpecificationRelationship>(m =>
                        m.Current.Description == newDescription &&
                        m.Current.Specification.Id == specificationId &&
                        m.Current.LastUpdated == _utcNow));
        }

        [TestMethod]
        public async Task GetFundingLineCalculations_GivenValidModelButDefinitionCouldNotBeFound_ReturnsNotFound()
        {
            string relationshipId = NewRandomString();

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns((DefinitionSpecificationRelationship)null);

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            IActionResult result = await service.GetFundingLineCalculations(relationshipId);

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(404);
        }

        [TestMethod]
        public async Task GetFundingLineCalculations_GivenValidModelButDefinitionNotReleasedDataType_ReturnsPreconditionFailed()
        {
            string datasetDefinitionId = NewRandomString();

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(datasetDefinitionId))
                .Returns(new DefinitionSpecificationRelationship
                {
                    Current = new DefinitionSpecificationRelationshipVersion
                    {
                        RelationshipType = DatasetRelationshipType.Uploaded
                    }
                });

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository);

            IActionResult result = await service.GetFundingLineCalculations(datasetDefinitionId);

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);
        }

        [TestMethod]
        public async Task GetFundingLineCalculations_GivenValidModel_ReturnsOk()
        {
            string datasetDefinitionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            uint fundingLineIdOne = NewRandomUint();
            uint fundingLineIdOneDup = fundingLineIdOne;
            uint fundingLineIdTwo = NewRandomUint();
            uint calculationIdOne = NewRandomUint();
            uint calculationIdOneDup = calculationIdOne;
            uint calculationIdTwo = NewRandomUint();
            string fundingLineOne = NewRandomString();
            string fundingLineTwo = NewRandomString();
            string calculationOne = NewRandomString();
            string calculationTwo = NewRandomString();
            string templateId = NewRandomString();

            ILogger logger = CreateLogger();

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new[] { new Reference(fundingStreamId, fundingStreamId) },
                FundingPeriod = new Reference(fundingPeriodId, fundingPeriodId),
                TemplateIds = new Dictionary<string, string>() { { fundingStreamId, templateId } }
            };

            Reference author = NewReference();
            PublishedSpecificationConfiguration currentPublishedSpecificationConfiguration = new PublishedSpecificationConfiguration()
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId,
                FundingLines = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = fundingLineIdOne, Name = fundingLineOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne)}
                },
                Calculations = new[]
                {
                    new PublishedSpecificationItem() { TemplateId = calculationIdOne, Name = calculationOne, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineOne), FieldType = FieldType.NullableOfDecimal},
                    new PublishedSpecificationItem() { TemplateId = calculationIdTwo, Name = calculationTwo, SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTwo), FieldType = FieldType.String}
                }
            };
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is<string>(specificationId))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));
            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(datasetDefinitionId))
                .Returns(new DefinitionSpecificationRelationship
                {
                    Current = new DefinitionSpecificationRelationshipVersion
                    {
                        RelationshipType = DatasetRelationshipType.ReleasedData,
                        Specification = new Reference { Id = specificationId, Name = specificationId },
                        PublishedSpecificationConfiguration = currentPublishedSpecificationConfiguration
                    }
                });
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetDistinctTemplateMetadataContents(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateId))
                .Returns(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK,
                new TemplateMetadataDistinctContents()
                {
                    FundingLines = new[]
                {
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineOne, TemplateLineId = fundingLineIdOne, Name = fundingLineOne},
                    new TemplateMetadataFundingLine() { FundingLineCode = fundingLineTwo, TemplateLineId = fundingLineIdTwo, Name = fundingLineTwo}
                },
                    Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationIdOne, Name = calculationOne, Type = Common.TemplateMetadata.Enums.CalculationType.Number}
                }
                }, null));

            DefinitionSpecificationRelationshipService service = CreateService(logger: logger, datasetRepository: datasetRepository,
                specificationsApiClient: specificationsApiClient, policiesApiClient: policiesApiClient);

            IActionResult result = await service.GetFundingLineCalculations(datasetDefinitionId);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            PublishedSpecificationConfiguration resultAsPublishedSpecificationConfiguration = objectResult.Value as PublishedSpecificationConfiguration;
            resultAsPublishedSpecificationConfiguration.SpecificationId.Should().Be(specificationId);
            resultAsPublishedSpecificationConfiguration.FundingStreamId.Should().Be(fundingStreamId);
            resultAsPublishedSpecificationConfiguration.FundingPeriodId.Should().Be(fundingPeriodId);

            resultAsPublishedSpecificationConfiguration.FundingLines.Should().HaveCount(2);
            resultAsPublishedSpecificationConfiguration.FundingLines.Single(s => s.TemplateId == fundingLineIdOne).IsObsolete.Should().BeFalse();
            resultAsPublishedSpecificationConfiguration.FundingLines.Single(s => s.TemplateId == fundingLineIdOne).IsSelected.Should().BeTrue();
            resultAsPublishedSpecificationConfiguration.FundingLines.Single(s => s.TemplateId == fundingLineIdTwo).IsObsolete.Should().BeFalse();
            resultAsPublishedSpecificationConfiguration.FundingLines.Single(s => s.TemplateId == fundingLineIdTwo).IsSelected.Should().BeFalse();

            resultAsPublishedSpecificationConfiguration.Calculations.Should().HaveCount(2);
            resultAsPublishedSpecificationConfiguration.Calculations.Single(s => s.TemplateId == calculationIdOne).IsObsolete.Should().BeFalse();
            resultAsPublishedSpecificationConfiguration.Calculations.Single(s => s.TemplateId == calculationIdOne).IsSelected.Should().BeTrue();
            resultAsPublishedSpecificationConfiguration.Calculations.Single(s => s.TemplateId == calculationIdTwo).IsObsolete.Should().BeTrue();
            resultAsPublishedSpecificationConfiguration.Calculations.Single(s => s.TemplateId == calculationIdTwo).IsSelected.Should().BeTrue();
        }

        private DefinitionSpecificationRelationshipService CreateService(
            IDatasetRepository datasetRepository = null,
            ILogger logger = null,
            ISpecificationsApiClient specificationsApiClient = null,
            IValidator<CreateDefinitionSpecificationRelationshipModel> relationshipModelValidator = null,
            IMessengerService messengerService = null,
            ICalcsRepository calcsRepository = null,
            ICacheProvider cacheProvider = null,
            IJobManagement jobManagement = null,
            IValidator<ValidateDefinitionSpecificationRelationshipModel> validateRelationshipModelValidator = null,
            IVersionRepository<DefinitionSpecificationRelationshipVersion> relationshipVersionRepository = null,
            IPoliciesApiClient policiesApiClient = null,
            IValidator<UpdateDefinitionSpecificationRelationshipModel> updateRelationshipModelValidator = null)
        {
            return new DefinitionSpecificationRelationshipService(
                datasetRepository ?? CreateDatasetRepository(),
                logger ?? CreateLogger(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                relationshipModelValidator ?? CreateRelationshipModelValidator(),
                messengerService ?? CreateMessengerService(),
                calcsRepository ?? CreateCalcsRepository(),
                cacheProvider ?? CreateCacheProvider(),
                DatasetsResilienceTestHelper.GenerateTestPolicies(),
                jobManagement ?? CreateJobManagement(),
                _dateTimeProvider,
                validateRelationshipModelValidator ?? CreateValidateRelationshipModelValidator(),
                relationshipVersionRepository ?? CreateRelationshipVersionRepository(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                updateRelationshipModelValidator ?? CreateUpdateRelationshipModelValidator());
        }

        private static IValidator<CreateDefinitionSpecificationRelationshipModel> CreateRelationshipModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CreateDefinitionSpecificationRelationshipModel> validator = Substitute.For<IValidator<CreateDefinitionSpecificationRelationshipModel>>();

            validator
               .ValidateAsync(Arg.Any<CreateDefinitionSpecificationRelationshipModel>())
               .Returns(validationResult);

            return validator;
        }

        private static IValidator<ValidateDefinitionSpecificationRelationshipModel> CreateValidateRelationshipModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<ValidateDefinitionSpecificationRelationshipModel> validator = Substitute.For<IValidator<ValidateDefinitionSpecificationRelationshipModel>>();

            validator
               .ValidateAsync(Arg.Any<ValidateDefinitionSpecificationRelationshipModel>())
               .Returns(validationResult);

            return validator;
        }

        private static IValidator<UpdateDefinitionSpecificationRelationshipModel> CreateUpdateRelationshipModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<UpdateDefinitionSpecificationRelationshipModel> validator = Substitute.For<IValidator<UpdateDefinitionSpecificationRelationshipModel>>();

            validator
               .ValidateAsync(Arg.Any<UpdateDefinitionSpecificationRelationshipModel>())
               .Returns(validationResult);

            return validator;
        }

        private static IDefinitionsService CreateDefinitionService()
        {
            return Substitute.For<IDefinitionsService>();
        }

        private static ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        private static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
        private static IMapper CreateMapper()
        {
            MapperConfiguration calculationsConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            return calculationsConfig.CreateMapper();
        }

        private static IDatasetService CreateDatasetService()
        {
            return Substitute.For<IDatasetService>();
        }

        private static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        private static IVersionRepository<DefinitionSpecificationRelationshipVersion> CreateRelationshipVersionRepository()
        {
            return Substitute.For<IVersionRepository<DefinitionSpecificationRelationshipVersion>>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();

            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }

        private DatasetVersion NewDatasetVersion(Action<DatasetVersionBuilder> setUp = null)
        {
            DatasetVersionBuilder datasetVersionBuilder = new DatasetVersionBuilder();

            setUp?.Invoke(datasetVersionBuilder);

            return datasetVersionBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private DefinitionSpecificationRelationshipVersion NewDefinitionSpecificationRelationshipVersion(Action<DefinitionSpecificationRelationshipVersionBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipVersionBuilder definitionSpecificationRelationshipVersionBuilder = new DefinitionSpecificationRelationshipVersionBuilder();

            setUp?.Invoke(definitionSpecificationRelationshipVersionBuilder);

            return definitionSpecificationRelationshipVersionBuilder.Build();
        }

        private DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder definitionSpecificationRelationshipBuilder = new DefinitionSpecificationRelationshipBuilder();

            setUp?.Invoke(definitionSpecificationRelationshipBuilder);

            return definitionSpecificationRelationshipBuilder.Build();
        }

        private DatasetRelationshipVersion NewDatasetRelationshipVersion(Action<DatasetRelationshipVersionBuilder> setUp = null)
        {
            DatasetRelationshipVersionBuilder datasetRelationshipVersionBuilder = new DatasetRelationshipVersionBuilder();

            setUp?.Invoke(datasetRelationshipVersionBuilder);

            return datasetRelationshipVersionBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
        private uint NewRandomUint() => (uint)new RandomNumberBetween(1, int.MaxValue);
        private DateTimeOffset NewRandomDateTime() => new RandomDateTime();

    }
}