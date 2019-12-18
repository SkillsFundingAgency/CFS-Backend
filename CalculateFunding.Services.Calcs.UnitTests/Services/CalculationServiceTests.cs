using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public partial class CalculationServiceTests
    {
        private const string UserId = "8bcd2782-e8cb-4643-8803-951d715fc202";
        private const string CalculationId = "3abc2782-e8cb-4643-8803-951d715fci23";
        private const string Username = "test-user";
        private const string SpecificationId = "spec-id-1";
        private const string CalculationName = "calc-name-1";
        private const string FundingStreamId = "fs-1";
        private const string DefaultSourceCode = "return 0";
        private const string Description = "test description";
        private const string CorrelationId = "4abc2782-e8cb-4643-8803-951d715fci29";

        private static CalculationService CreateCalculationService(
            IMapper mapper = null,
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null,
            ISearchRepository<CalculationIndex> searchRepository = null,
            IValidator<Calculation> calcValidator = null,
            IBuildProjectsService buildProjectsService = null,
            IPoliciesApiClient policiesApiClient = null,
            ICacheProvider cacheProvider = null,
            ICalcsResiliencePolicies resiliencePolicies = null,
            IVersionRepository<CalculationVersion> calculationVersionRepository = null,
            IJobsApiClient jobsApiClient = null,
            ISourceCodeService sourceCodeService = null,
            IFeatureToggle featureToggle = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = null,
            IValidator<CalculationCreateModel> calculationCreateModelValidator = null,
            IValidator<CalculationEditModel> calculationEditModelValidator = null,
            ISpecificationsApiClient specificationsApiClient = null)
        {
            CalculationNameInUseCheck calculationNameInUseCheck = new CalculationNameInUseCheck(calculationsRepository ?? CreateCalculationsRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies());

            InstructionAllocationJobCreation instructionAllocationJobCreation = new InstructionAllocationJobCreation(calculationsRepository ?? CreateCalculationsRepository(),
                resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                logger ?? CreateLogger(),
                jobsApiClient ?? CreateJobsApiClient());

            return new CalculationService
                (
                calculationsRepository ?? CreateCalculationsRepository(),
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                buildProjectsService ?? CreateBuildProjectsService(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                cacheProvider ?? CreateCacheProvider(),
                resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                calculationVersionRepository ?? CreateCalculationVersionRepository(),
                sourceCodeService ?? CreateSourceCodeService(),
                featureToggle ?? CreateFeatureToggle(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(),
                calculationCodeReferenceUpdate ?? CreateCalculationCodeReferenceUpdate(),
                calculationCreateModelValidator ?? CreateCalculationCreateModelValidator(),
                calculationEditModelValidator ?? CreateCalculationEditModelValidator(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                calculationNameInUseCheck,
                instructionAllocationJobCreation,
                new CreateCalculationService(calculationNameInUseCheck,
                    calculationsRepository ?? CreateCalculationsRepository(),
                    calculationVersionRepository ?? CreateCalculationVersionRepository(),
                    resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                    calculationCreateModelValidator ?? CreateCalculationCreateModelValidator(),
                    cacheProvider ?? CreateCacheProvider(),
                    searchRepository ?? CreateSearchRepository(),
                    logger ?? CreateLogger(),
                    instructionAllocationJobCreation));
        }

        private static ICalculationCodeReferenceUpdate CreateCalculationCodeReferenceUpdate()
        {
            return Substitute.For<ICalculationCodeReferenceUpdate>();
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        private static ISourceCodeService CreateSourceCodeService()
        {
            return Substitute.For<ISourceCodeService>();
        }

        private static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        private static IVersionRepository<CalculationVersion> CreateCalculationVersionRepository()
        {
            return Substitute.For<IVersionRepository<CalculationVersion>>();
        }

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static IBuildProjectsService CreateBuildProjectsService()
        {
            return Substitute.For<IBuildProjectsService>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration resultsConfig = new MapperConfiguration(c =>
            {
                // c.AddProfile<PolicyMappingProfile>();
            });

            return resultsConfig.CreateMapper();
        }

        private static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        private static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        private static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        private static IValidator<Calculation> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<Calculation> validator = Substitute.For<IValidator<Calculation>>();

            validator
               .ValidateAsync(Arg.Any<Calculation>())
               .Returns(validationResult);

            return validator;
        }

        private static IValidator<CalculationCreateModel> CreateCalculationCreateModelValidator(ValidationResult validationResult = null)
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

        private static IValidator<CalculationEditModel> CreateCalculationEditModelValidator(ValidationResult validationResult = null)
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

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private static Calculation CreateCalculation()
        {
            return new Calculation
            {
                Id = CalculationId,

                SpecificationId = "any-spec-id",

                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Draft,
                    Description = "description",
                    Name = "Test Calc Name"
                },
                FundingStreamId = "funding stream-id"
            };
        }

        private static CalculationCreateModel CreateCalculationCreateModel()
        {
            return new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                SpecificationName = $"{SpecificationId}_specificationName",
                FundingStreamId = FundingStreamId,
                FundingStreamName = $"{FundingStreamId}_fundingStreamName", 
                Name = CalculationName,
                ValueType = CalculationValueType.Currency,
                SourceCode = DefaultSourceCode,
                Description = Description
            };
        }

        private static CalculationEditModel CreateCalculationEditModel()
        {
            return new CalculationEditModel
            {
                Name = CalculationName,
                ValueType = CalculationValueType.Currency,
                SourceCode = DefaultSourceCode,
                Description = Description
            };
        }

        private static Reference CreateAuthor()
        {
            return new Reference(UserId, Username);
        }
    }
}
