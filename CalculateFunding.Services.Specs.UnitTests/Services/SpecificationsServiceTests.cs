using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecificationVersion = CalculateFunding.Models.Specs.SpecificationVersion;


namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    [TestClass]
    public partial class SpecificationsServiceTests
    {
        private ISpecificationIndexer _specificationIndexer;
        
        const string FundingStreamId = "YAPGG";
        const string SpecificationId = "ffa8ccb3-eb8e-4658-8b3f-f1e4c3a8f313";
        const string PolicyId = "dda8ccb3-eb8e-4658-8b3f-f1e4c3a8f322";
        const string CalculationId = "22a6eeaf-e1a0-476e-9cf9-8aa6c51293433";
        const string FundingPeriodId = "18/19";
        const string SpecificationName = "Test Spec 001";
        const string PolicyName = "Test Policy 001";
        const string CalculationName = "Test Calc 001";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        const string SfaCorrelationId = "c625c3f9-6ce8-4f1f-a3a3-4611f1dc3881";
        const string RelationshipId = "cca8ccb3-eb8e-4658-8b3f-f1e4c3a8f419";
        private readonly Specification _specification;
        private readonly ILogger _logger = CreateLogger();
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IVersionRepository<Models.Specs.SpecificationVersion> _versionRepository;

        private ISpecificationTemplateVersionChangedHandler _templateVersionChangedHandler;
        private IResultsApiClient _resultsApiClient;        
        

        [TestInitialize]
        public void SetUp()
        {
            specificationsRepository = CreateSpecificationsRepository();

            policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.GetFundingPeriods()
                .Returns(new ApiResponse<IEnumerable<FundingPeriod>>(HttpStatusCode.OK, new[]
                {
                    new FundingPeriod
                    {
                        Id = ExpectedFundingPeriodId,
                        Period = ExpectedFundingPeriodName
                    },
                    new FundingPeriod
                    {
                        Id = "FIRST DIFFERENT ID",
                        Period = "A DIFFERENT PERIOD"
                    },
                    new FundingPeriod
                    {
                        Id = "A DIFFERENT ID",
                        Period = ExpectedFundingPeriodName
                    }
                }));

            mapper = CreateMapper();
            mapper.Map<SpecificationSummary>(_specWithFundingPeriodAndFundingStream)
                .Returns(MapSpecification(_specWithFundingPeriodAndFundingStream));
            mapper.Map<SpecificationSummary>(_specWithFundingPeriodAndFundingStream2)
                .Returns(MapSpecification(_specWithFundingPeriodAndFundingStream2));
            mapper.Map<SpecificationSummary>(_specWithNoFundingStream)
                .Returns(MapSpecification(_specWithNoFundingStream));
            
            _specificationIndexer = Substitute.For<ISpecificationIndexer>();
            _resultsApiClient = Substitute.For<IResultsApiClient>();
            _templateVersionChangedHandler = Substitute.For<ISpecificationTemplateVersionChangedHandler>();
        }

        private SpecificationsService CreateService(
            IMapper mapper = null,
            ISpecificationsRepository specificationsRepository = null,
            IPoliciesApiClient policiesApiClient = null,
            ILogger logs = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null,
            IMessengerService messengerService = null,
            ISearchRepository<SpecificationIndex> searchRepository = null,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator = null,
            ICacheProvider cacheProvider = null,
            IValidator<SpecificationEditModel> specificationEditModelValidator = null,
            IResultsRepository resultsRepository = null,
            IVersionRepository<Models.Specs.SpecificationVersion> specificationVersionRepository = null,
            IQueueCreateSpecificationJobActions queueCreateSpecificationJobActions = null,
            IQueueDeleteSpecificationJobActions queueDeleteSpecificationJobActions = null,
            IFeatureToggle featureToggle = null,
            ICalculationsApiClient calcsApiClient = null,
            IProvidersApiClient providersApiClient = null,
            IValidator<AssignSpecificationProviderVersionModel> assignSpecificationProviderVersionModelValidator = null)
        {
            return new SpecificationsService(
                mapper ?? CreateMapper(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                logs ?? CreateLogger(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(),
                messengerService ?? CreateMessengerService(),
                searchRepository ?? CreateSearchRepository(),
                assignDefinitionRelationshipMessageValidator ?? CreateAssignDefinitionRelationshipMessageValidator(),
                cacheProvider ?? CreateCacheProvider(),
                specificationEditModelValidator ?? CreateEditSpecificationValidator(),
                resultsRepository ?? CreateResultsRepository(),
                specificationVersionRepository ?? CreateVersionRepository(),
                SpecificationsResilienceTestHelper.GenerateTestPolicies(),
                queueCreateSpecificationJobActions ?? Substitute.For<IQueueCreateSpecificationJobActions>(),
                queueDeleteSpecificationJobActions ?? Substitute.For<IQueueDeleteSpecificationJobActions>(),
                calcsApiClient ?? CreateCalcsApiClient(),
                featureToggle ?? Substitute.For<IFeatureToggle>(),
                providersApiClient ?? Substitute.For<IProvidersApiClient>(),
                _specificationIndexer,
                _resultsApiClient,
                _templateVersionChangedHandler,
                assignSpecificationProviderVersionModelValidator ?? CreateAssignSpecificationProviderVersionModelValidator());
        }

        private async Task AndAMergeSpecificationInformationJobWasQueued(SpecificationVersion specification)
        {
            await _resultsApiClient.Received(1)
                .QueueMergeSpecificationInformationForProviderJobForAllProviders(Arg.Is<SpecificationInformation>(_ =>
                    _.Id == specification.Id &&
                    _.Name == specification.Name &&
                    _.LastEditDate == specification.Date &&
                    _.FundingPeriodId == specification.FundingPeriod.Id));
        }

        protected IVersionRepository<Models.Specs.SpecificationVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<Models.Specs.SpecificationVersion>>();
        }

        protected IResultsRepository CreateResultsRepository()
        {
            return Substitute.For<IResultsRepository>();
        }

        protected ICalculationsApiClient CreateCalcsApiClient()
        {
            return Substitute.For<ICalculationsApiClient>();
        }

        protected IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        protected IMapper CreateImplementedMapper()
        {
            MapperConfiguration mappingConfiguration = new MapperConfiguration(
                c =>
                {
                    c.AddProfile<SpecificationsMappingProfile>();                  
                }
            );
            IMapper mapper = mappingConfiguration.CreateMapper();
            return mapper;
        }

        protected IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        protected ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        protected ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        protected IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        protected IProvidersApiClient CreateProvidersApiClient()
        {
            return Substitute.For<IProvidersApiClient>();
        }

        protected IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        protected static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected ISearchRepository<SpecificationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<SpecificationIndex>>();
        }

        protected ITemplateMetadataGenerator CreateTemplateMetadataGenerator()
        {
            return Substitute.For<ITemplateMetadataGenerator>();
        }

        protected IVersionRepository<Models.Specs.SpecificationVersion> CreateSpecificationVersionRepository()
        {
            return Substitute.For<IVersionRepository<Models.Specs.SpecificationVersion>>();
        }

        protected IValidator<SpecificationCreateModel> CreateSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<SpecificationCreateModel> validator = Substitute.For<IValidator<SpecificationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        protected IValidator<SpecificationEditModel> CreateEditSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<SpecificationEditModel> validator = Substitute.For<IValidator<SpecificationEditModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationEditModel>())
               .Returns(validationResult);

            return validator;
        }

        protected IValidator<AssignDefinitionRelationshipMessage> CreateAssignDefinitionRelationshipMessageValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<AssignDefinitionRelationshipMessage> validator = Substitute.For<IValidator<AssignDefinitionRelationshipMessage>>();

            validator
               .ValidateAsync(Arg.Any<AssignDefinitionRelationshipMessage>())
               .Returns(validationResult);

            return validator;
        }

        protected IValidator<AssignSpecificationProviderVersionModel> CreateAssignSpecificationProviderVersionModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<AssignSpecificationProviderVersionModel> validator = Substitute.For<IValidator<AssignSpecificationProviderVersionModel>>();

            validator
               .ValidateAsync(Arg.Any<AssignSpecificationProviderVersionModel>())
               .Returns(validationResult);

            return validator;
        }

        protected Specification CreateSpecification()
        {
            return new Specification()
            {
                Id = SpecificationId,
                Name = "Spec Name",
                Current = new Models.Specs.SpecificationVersion()
                {
                    Name = "Spec name",
                    ProviderVersionId = "Provider version 1",
                    FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
                    Author = new Reference("author@dfe.gov.uk", "Author Name"),
                    DataDefinitionRelationshipIds = new List<string>()
                       {
                           "dr1",
                           "dr2"
                       },
                    Description = "Specification Description",
                    FundingPeriod = new Reference("FP1", "Funding Period"),
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    Version = 1,
                    ProviderSource = Models.Providers.ProviderSource.CFS,
                    ProviderSnapshotId = 0
                }
            };
        }
        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private PublishedFundingTemplate NewPublishedFundingTemplate(Action<PublishedFundingTemplateBuilder> setUp = null)
        {
            PublishedFundingTemplateBuilder publishedFundingTemplateBuilder = new PublishedFundingTemplateBuilder();

            setUp?.Invoke(publishedFundingTemplateBuilder);

            return publishedFundingTemplateBuilder.Build();
        }

        private Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);

            return specificationBuilder.Build();
        }

        private IEnumerable<Specification> NewSpecifications(params Action<SpecificationBuilder>[] setUps)
        {
            return setUps.Select(NewSpecification);
        }

        private string NewRandomString() => new RandomString();

    }
}
