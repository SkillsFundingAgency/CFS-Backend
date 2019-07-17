using System.Collections.Generic;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using FluentValidation;
using FluentValidation.Results;
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
        const string AllocationLineId = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345";
        const string CalculationId = "22a6eeaf-e1a0-476e-9cf9-8aa6c51293433";
        const string FundingPeriodId = "18/19";
        const string SpecificationName = "Test Spec 001";
        const string PolicyName = "Test Policy 001";
        const string CalculationName = "Test Calc 001";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        const string SfaCorrelationId = "c625c3f9-6ce8-4f1f-a3a3-4611f1dc3881";
        const string RelationshipId = "cca8ccb3-eb8e-4658-8b3f-f1e4c3a8f419";

        private SpecificationsService CreateService(
            IMapper mapper = null,
            ISpecificationsRepository specificationsRepository = null,
            IPoliciesApiClient policiesApiClient = null,
            ILogger logs = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null,
            IValidator<CalculationCreateModel> calculationCreateModelValidator = null,
            IMessengerService messengerService = null, ServiceBusSettings EventHubSettings = null,
            ISearchRepository<SpecificationIndex> searchRepository = null,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator = null,
            ICacheProvider cacheProvider = null,
            IValidator<SpecificationEditModel> specificationEditModelValidator = null,
            IValidator<CalculationEditModel> calculationEditModelValidator = null,
            IResultsRepository resultsRepository = null,
            IVersionRepository<SpecificationVersion> specificationVersionRepository = null,
            IFeatureToggle featureToggle = null,
            IJobsApiClient jobsApiClient = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                logs ?? CreateLogger(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(),
                calculationCreateModelValidator ?? CreateCalculationValidator(),
                messengerService ?? CreateMessengerService(),
                searchRepository ?? CreateSearchRepository(),
                assignDefinitionRelationshipMessageValidator ?? CreateAssignDefinitionRelationshipMessageValidator(),
                cacheProvider ?? CreateCacheProvider(),
                specificationEditModelValidator ?? CreateEditSpecificationValidator(),
                calculationEditModelValidator ?? CreateEditCalculationValidator(),
                resultsRepository ?? CreateResultsRepository(),
                specificationVersionRepository ?? CreateVersionRepository(),
                featureToggle ?? CreateFeatureToggle(),
                jobsApiClient ?? CreateJobsApiClient(),
                SpecificationsResilienceTestHelper.GenerateTestPolicies()
                );
        }

        protected IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        protected IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();

            return featureToggle;
        }

        protected IVersionRepository<SpecificationVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<SpecificationVersion>>();
        }

        protected IResultsRepository CreateResultsRepository()
        {
            return Substitute.For<IResultsRepository>();
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
                    c.AddProfile<PolicyMappingProfile>();
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

        protected ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        protected ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        protected ISearchRepository<SpecificationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<SpecificationIndex>>();
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

        protected IValidator<CalculationCreateModel> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CalculationCreateModel> validator = Substitute.For<IValidator<CalculationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        protected IValidator<CalculationEditModel> CreateEditCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CalculationEditModel> validator = Substitute.For<IValidator<CalculationEditModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationEditModel>())
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
                Current = new SpecificationVersion()
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

        protected IEnumerable<FundingStream> CreateFundingStreams()
        {
            return new[]
            {
                new FundingStream
                {
                    Id = "PSG",
                    Name = "PE and Sport Premium Grant",
                    ShortName = "PE and Sport",
                    PeriodType = new PeriodType
                    {
                        Id = "AC",
                        StartDay = 1,
                        StartMonth = 9,
                        EndDay = 31,
                        EndMonth = 8,
                        Name = "Academies Academic Year"
                    },
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "PSG-NMSS",
                            Name = "Non-maintained Special Schools",
                            FundingRoute = FundingRoute.Provider,
                            IsContractRequired = true,
                            ShortName = "NMSS"
                        },
                        new AllocationLine
                        {
                            Id = "PSG-ACAD",
                            Name = "Academies",
                            FundingRoute = FundingRoute.Provider,
                            IsContractRequired = false,
                            ShortName = "Acad"
                        },
                         new AllocationLine
                        {
                            Id = "PSG-LAMS",
                            Name = "Maintained Schools",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = false,
                            ShortName = "MS"
                        }
                    }
                }
            };
        }
    }
}
