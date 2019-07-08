using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
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
        const string UserId = "8bcd2782-e8cb-4643-8803-951d715fc202";
        const string CalculationId = "3abc2782-e8cb-4643-8803-951d715fci23";
        const string Username = "test-user";

        private static CalculationService CreateCalculationService(
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null,
            ISearchRepository<CalculationIndex> searchRepository = null,
            IValidator<Calculation> calcValidator = null,
            IBuildProjectsService buildProjectsService = null,
            ISpecificationRepository specificationRepository = null,
            IPoliciesRepository policiesRepository = null,
            ICacheProvider cacheProvider = null,
            ICalcsResiliencePolicies resiliencePolicies = null,
            IVersionRepository<CalculationVersion> calculationVersionRepository = null,
            IJobsApiClient jobsApiClient = null,
            ISourceCodeService sourceCodeService = null,
            IFeatureToggle featureToggle = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = null)
        {
            return new CalculationService
                (calculationsRepository ?? CreateCalculationsRepository(),
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                calcValidator ?? CreateCalculationValidator(),
                buildProjectsService ?? CreateBuildProjectsService(),
                specificationRepository ?? CreateSpecificationRepository(),
                policiesRepository ?? CreatePoliciesRepository(),
                cacheProvider ?? CreateCacheProvider(),
                resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                calculationVersionRepository ?? CreateCalculationVersionRepository(),
                jobsApiClient ?? CreateJobsApiClient(),
                sourceCodeService ?? CreateSourceCodeService(),
                featureToggle ?? CreateFeatureToggle(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(),
                calculationCodeReferenceUpdate ?? CreateCalculationCodeReferenceUpdate());
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

        private static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        private static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        private static IPoliciesRepository CreatePoliciesRepository()
        {
            return Substitute.For<IPoliciesRepository>();
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

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static Calculation CreateCalculation()
        {
            return new Calculation
            {
                Id = CalculationId,
                Name = "Test Calc Name",
                CalculationSpecification = new Reference
                {
                    Id = "any-calc-id",
                    Name = "Test Calc Name",
                },
                SpecificationId = "any-spec-id",
                FundingPeriod = new Reference
                {
                    Id = "18/19",
                    Name = "2018/2019"
                },
                AllocationLine = new Reference
                {
                    Id = "test-alloc-id",
                    Name = "test-alloc-name"
                },
                Policies = new List<Reference>
                {
                    new Reference
                    {
                        Id = "policy-id",
                        Name = "policy-name"
                    }
                },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Draft,
                },
                FundingStream = new Reference
                {
                    Id = "funding stream-id",
                    Name = "funding-stream-name"
                }
            };
        }
    }
}
