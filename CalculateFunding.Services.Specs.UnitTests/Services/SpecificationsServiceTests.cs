using System.Collections.Generic;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;

using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;


namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    [TestClass]
    public partial class SpecificationsServiceTests
    {
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
        readonly HttpContext _context = Substitute.For<HttpContext>();
        readonly HttpRequest _request = Substitute.For<HttpRequest>();
        private readonly Specification _specification;
        private readonly ILogger _logger = CreateLogger();
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<SpecificationIndex> _searchRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IVersionRepository<Models.Specs.SpecificationVersion> _versionRepository;

        private SpecificationsService CreateService(
            IMapper mapper = null,
            ISpecificationsRepository specificationsRepository = null,
            IPoliciesApiClient policiesApiClient = null,
            ILogger logs = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null,
            IMessengerService messengerService = null, ServiceBusSettings EventHubSettings = null,
            ISearchRepository<SpecificationIndex> searchRepository = null,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator = null,
            ICacheProvider cacheProvider = null,
            IValidator<SpecificationEditModel> specificationEditModelValidator = null,
            IResultsRepository resultsRepository = null,
            IVersionRepository<Models.Specs.SpecificationVersion> specificationVersionRepository = null,
            IQueueCreateSpecificationJobActions queueCreateSpecificationJobActions = null,
            IQueueDeleteSpecificationJobActions queueDeleteSpecificationJobActions = null,
            IFeatureToggle featureToggle = null,
            ICalculationsApiClient calcsApiClient = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(),
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
                Substitute.For<IHostingEnvironment>(),
                featureToggle ?? Substitute.For<IFeatureToggle>());
        }

        protected IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
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

        protected Specification CreateSpecification()
        {
            return new Specification()
            {
                Id = SpecificationId,
                Name = "Spec Name",
                Current = new Models.Specs.SpecificationVersion()
                {
                    Name = "Spec name",
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
                    Version = 1
                }
            };
        }
    }
}
