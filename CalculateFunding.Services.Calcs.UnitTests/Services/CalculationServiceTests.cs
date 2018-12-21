using System.Collections.Generic;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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

        static CalculationService CreateCalculationService(
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null,
            ITelemetry telemetry = null,
            ISearchRepository<CalculationIndex> searchRepository = null,
            IValidator<Calculation> calcValidator = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ICompilerFactory compilerFactory = null,
            IMessengerService messengerService = null,
            ICodeMetadataGeneratorService codeMetadataGenerator = null,
            ISpecificationRepository specificationRepository = null,
            ICacheProvider cacheProvider = null,
            ICalcsResilliencePolicies resilliencePolicies = null,
            IVersionRepository<CalculationVersion> calculationVersionRepository = null,
            IJobsRepository jobsRepository = null,
            IFeatureToggle featureToggle = null)
        {
            return new CalculationService
                (calculationsRepository ?? CreateCalculationsRepository(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                searchRepository ?? CreateSearchRepository(),
                calcValidator ?? CreateCalculationValidator(),
                buildProjectsRepository ?? CreateBuildProjectsRepository(),
                sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(),
                compilerFactory ?? CreateCompilerFactory(),
                messengerService ?? CreateMessengerService(),
                codeMetadataGenerator ?? CreateCodeMetadataGenerator(),
                specificationRepository ?? CreateSpecificationRepository(),
                cacheProvider ?? CreateCacheProvider(),
                resilliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                calculationVersionRepository ?? CreateCalculationVersionRepository(),
                jobsRepository ?? CreateJobsRepository(),
                featureToggle ?? CreateFeatureToggle());
        }

        static IJobsRepository CreateJobsRepository()
        {
            return Substitute.For<IJobsRepository>();
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        static IVersionRepository<CalculationVersion> CreateCalculationVersionRepository()
        {
            return Substitute.For<IVersionRepository<CalculationVersion>>();
        }

        static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider()
        {
            return Substitute.For<ISourceFileGeneratorProvider>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        static ICodeMetadataGeneratorService CreateCodeMetadataGenerator()
        {
            return Substitute.For<ICodeMetadataGeneratorService>();
        }

        static ICompilerFactory CreateCompilerFactory()
        {
            ICompiler compiler = Substitute.For<ICompiler>();

            ICompilerFactory compilerFactory = Substitute.For<ICompilerFactory>();
            compilerFactory
                .GetCompiler(Arg.Any<IEnumerable<SourceFile>>())
                .Returns(compiler);

            return compilerFactory;
        }

        static IValidator<Calculation> CreateCalculationValidator(ValidationResult validationResult = null)
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

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static Calculation CreateCalculation()
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
