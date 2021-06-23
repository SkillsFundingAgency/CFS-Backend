using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenValidInputProvided_ThenSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            Reference user = new Reference(UserId, Username);
            
            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithDefaultTemplateVersion(NewRandomString()));

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(FundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationSummary>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => _.Name == specificationCreateModel.Name &&
                                                  _.Id.IsNotNullOrWhitespace()));

            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1 &&
                       m.ProviderSource == Models.Providers.ProviderSource.CFS 
                   ));

            await createSpecificationJobAction
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                             m.Description == "Specification Description" &&
                             m.FundingPeriod.Id == "fp1" &&
                             m.FundingPeriod.Name == "Funding Period 1" &&
                             m.FundingStreams.Any() &&
                             m.Name == "Specification Name" &&
                             m.Version == 1 &&
                             m.ProviderSource == Models.Providers.ProviderSource.CFS
                    ),
                    Arg.Is<Reference>(author => author.Id == UserId &&
                                              author.Name == Username),
                    Arg.Any<string>());
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenInvalidInputProvided_WithFDZFundingConfig_ThenSpecificationProviderSnapshotIdIsSet()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
                ProviderSnapshotId = null
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithDefaultTemplateVersion(NewRandomString())
                .WithProviderSource(ProviderSource.FDZ));

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>();
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenValidInputProvided_WithFDZFundingConfig_ThenSpecificationProviderSnapshotIdIsSet()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
                ProviderSnapshotId = 1
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithDefaultTemplateVersion(NewRandomString())
                .WithProviderSource(ProviderSource.FDZ));

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(FundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId,
                ProviderSource = Models.Providers.ProviderSource.FDZ,
                ProviderSnapshotId = 1
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationSummary>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId &&
                   s.Current.ProviderSnapshotId == 1 &&
                   s.Current.ProviderSource == Models.Providers.ProviderSource.FDZ));

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => _.Name == specificationCreateModel.Name &&
                                                  _.Id.IsNotNullOrWhitespace()));

            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1 &&
                       m.ProviderSource == Models.Providers.ProviderSource.FDZ &&
                       m.ProviderSnapshotId == 1
                   ));

            await createSpecificationJobAction
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                             m.Description == "Specification Description" &&
                             m.FundingPeriod.Id == "fp1" &&
                             m.FundingPeriod.Name == "Funding Period 1" &&
                             m.FundingStreams.Any() &&
                             m.Name == "Specification Name" &&
                             m.Version == 1 &&
                             m.ProviderSource == Models.Providers.ProviderSource.FDZ &&
                             m.ProviderSnapshotId == 1
                    ),
                    Arg.Is<Reference>(author => author.Id == UserId &&
                                              author.Name == Username),
                    Arg.Any<string>());
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenAssignedTemplateSet_AndTemplateVersionNotExists_ThenSpecificationCreationFailed()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";
            const string templateVersion = "2.0";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
                AssignedTemplateIds = new Dictionary<string, string>
                {
                    { fundingStreamId, templateVersion}
                }
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>> publishedFundingTemplatesResponse
                = new ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>>(HttpStatusCode.OK, Enumerable.Empty<PolicyModels.PublishedFundingTemplate>());

            policiesApiClient
                .GetFundingTemplates(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(publishedFundingTemplatesResponse);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(new ApiResponse<FundingTemplateContents>(HttpStatusCode.NotFound, null, null));

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"No published funding template returned for funding stream id '{fundingStreamId}' and funding period id " +
                            $"'{fundingPeriodId}' and template version '{templateVersion}'");
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenAssignedTemplateSet_AndTemplateVersionExists_ThenTrimmedSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";
            const string templateVersion = "2.0";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();
            ICalculationsApiClient calculationsApiClient = CreateCalcsApiClient();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction,
                calcsApiClient: calculationsApiClient);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "   Specification Name   ",
                Description = "Specification Description",
                FundingPeriodId = fundingPeriodId,
                FundingStreamIds = new List<string>() { fundingStreamId },
                AssignedTemplateIds = new Dictionary<string, string>
                {
                    { fundingStreamId, templateVersion }
                }
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            PublishedFundingTemplate publishedFundingTemplateOne = NewPublishedFundingTemplate(_ => _.WithTemplateVersion("9.9"));
            PublishedFundingTemplate publishedFundingTemplateTwo = NewPublishedFundingTemplate(_ => _.WithTemplateVersion("10.0"));

            ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>> publishedFundingTemplatesResponse
                = new ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>>(
                    HttpStatusCode.OK,
                    new[] { publishedFundingTemplateOne, publishedFundingTemplateTwo });

            policiesApiClient
                .GetFundingTemplates(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(publishedFundingTemplatesResponse);

            ApiResponse<FundingTemplateContents> publishedFundingTemplateResponse
                = new ApiResponse<FundingTemplateContents>(
                    HttpStatusCode.OK,
                    new FundingTemplateContents());

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(publishedFundingTemplateResponse);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(fundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name.Trim() &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationSummary>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name.Trim() &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => _.Name == specificationCreateModel.Name.Trim() &&
                                                  _.Id.IsNotNullOrWhitespace()));
            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1 &&
                       m.ProviderSource == Models.Providers.ProviderSource.CFS
                   ));

            await createSpecificationJobAction
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                             m.Description == "Specification Description" &&
                             m.FundingPeriod.Id == "fp1" &&
                             m.FundingPeriod.Name == "Funding Period 1" &&
                             m.FundingStreams.Any() &&
                             m.Name == "Specification Name" &&
                             m.Version == 1 &&
                             m.ProviderSource == Models.Providers.ProviderSource.CFS
                    ),
                    Arg.Is<Reference>(author => author.Id == UserId &&
                                              author.Name == Username),
                    Arg.Any<string>());

            SpecificationSummary specificationSummary = (result as OkObjectResult).Value as SpecificationSummary;

            await calculationsApiClient
                .Received(1)
                .ProcessTemplateMappings(Arg.Is(specificationSummary.Id), templateVersion, fundingStreamId);
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenFundingConfigurationDefaultTemplateVersionNotSet_AndFundingTemplateNotExists_ThenSpecificationCreationFailed()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>> publishedFundingTemplatesResponse 
                = new ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>>(HttpStatusCode.OK, Enumerable.Empty<PolicyModels.PublishedFundingTemplate>());

            policiesApiClient
                .GetFundingTemplates(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(publishedFundingTemplatesResponse);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Default Template Version is empty for funding stream id '{fundingStreamId}' and funding period id '{fundingPeriodId}'");
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenFundingConfigurationDefaultTemplateVersionNotSet_AndFundingTemplateExists_ThenTrimmedSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();
            ICalculationsApiClient calculationsApiClient = CreateCalcsApiClient();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction,
                calcsApiClient: calculationsApiClient);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "   Specification Name   ",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            PublishedFundingTemplate publishedFundingTemplateOne = NewPublishedFundingTemplate(_ => _.WithTemplateVersion("9.9"));
            PublishedFundingTemplate publishedFundingTemplateTwo = NewPublishedFundingTemplate(_ => _.WithTemplateVersion("10.0"));

            ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>> publishedFundingTemplatesResponse
                = new ApiResponse<IEnumerable<PolicyModels.PublishedFundingTemplate>>(
                    HttpStatusCode.OK, 
                    new[] { publishedFundingTemplateOne , publishedFundingTemplateTwo });

            policiesApiClient
                .GetFundingTemplates(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(publishedFundingTemplatesResponse);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(fundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name.Trim() &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationSummary>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name.Trim() &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));

            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => _.Name == specificationCreateModel.Name.Trim() &&
                                                  _.Id.IsNotNullOrWhitespace()));
            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1 &&
                       m.ProviderSource == Models.Providers.ProviderSource.CFS &&
                       m.TemplateIds[fundingStreamId] == "10.0"
                   ));

            await createSpecificationJobAction
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                             m.Description == "Specification Description" &&
                             m.FundingPeriod.Id == "fp1" &&
                             m.FundingPeriod.Name == "Funding Period 1" &&
                             m.FundingStreams.Any() &&
                             m.Name == "Specification Name" &&
                             m.Version == 1 &&
                             m.ProviderSource == Models.Providers.ProviderSource.CFS
                    ),
                    Arg.Is<Reference>(author => author.Id == UserId &&
                                              author.Name == Username),
                    Arg.Any<string>());

            SpecificationSummary specificationSummary = (result as OkObjectResult).Value as SpecificationSummary;

            await calculationsApiClient
                .Received(1)
                .ProcessTemplateMappings(Arg.Is(specificationSummary.Id), "10.0", fundingStreamId);
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenValidInputProvided_ThenTrimmedSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IQueueCreateSpecificationJobActions createSpecificationJobAction = Substitute.For<IQueueCreateSpecificationJobActions>();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                queueCreateSpecificationJobActions: createSpecificationJobAction);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "   Specification Name   ",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            string templateVersion = NewRandomString();

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithDefaultTemplateVersion(templateVersion));

            ApiResponse<FundingConfiguration> fundingConfigResponse = new ApiResponse<FundingConfiguration>(HttpStatusCode.OK, fundingConfiguration);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingConfiguration(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId))
                .Returns(fundingConfigResponse);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(fundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name.Trim() &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationSummary>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name.Trim() &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));
            
            await _specificationIndexer
                .Received(1)
                .Index(Arg.Is<Specification>(_ => _.Name == specificationCreateModel.Name.Trim() &&
                                                  _.Id.IsNotNullOrWhitespace()));
            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1 &&
                       m.ProviderSource == Models.Providers.ProviderSource.CFS &&
                       m.TemplateIds[fundingStreamId] == templateVersion
                   ));

            await createSpecificationJobAction
                .Received(1)
                .Run(Arg.Is<SpecificationVersion>(
                        m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                             m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                             m.Description == "Specification Description" &&
                             m.FundingPeriod.Id == "fp1" &&
                             m.FundingPeriod.Name == "Funding Period 1" &&
                             m.FundingStreams.Any() &&
                             m.Name == "Specification Name" &&
                             m.Version == 1 &&
                             m.ProviderSource == Models.Providers.ProviderSource.CFS
                    ),
                    Arg.Is<Reference>(author => author.Id == UserId &&
                                              author.Name == Username),
                    Arg.Any<string>());
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenFundingStreamIDIsProvidedButDoesNotExist_ThenPreConditionFailedReturned()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingStreamNotFoundId = "notfound";

            const string fundingPeriodId = "fp1";


            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                searchRepository: searchRepository,
                mapper: mapper);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId, fundingStreamNotFoundId, },
            };

            Reference user = new Reference(UserId, Username);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Specification)null);

            PolicyModels.FundingPeriod fundingPeriod = new PolicyModels.FundingPeriod
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, fundingPeriod);

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            policiesApiClient
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriodResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStreamResponse);

            policiesApiClient
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId))
                .Returns(new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, null, null));

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, user, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Unable to find funding stream with ID 'notfound'.");

            await policiesApiClient
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId));

            await policiesApiClient
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamId));
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenInvalidInputProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("fundingStreamId", "Test"));

            IValidator<SpecificationCreateModel> validator = CreateSpecificationValidator(validationResult);

            SpecificationsService specificationsService = CreateService(specificationCreateModelvalidator: validator);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = null,
                FundingStreamIds = new List<string>() { },
            };

            // Act
            IActionResult result = await specificationsService.CreateSpecification(specificationCreateModel, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SerializableError>()
                .Which
                .Should()
                .HaveCount(1);

            await validator
                .Received(1)
                .ValidateAsync(Arg.Any<SpecificationCreateModel>());
        }
    }
}
