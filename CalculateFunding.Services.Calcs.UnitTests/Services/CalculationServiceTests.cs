using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
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
using System.Collections.Generic;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public partial class CalculationServiceTests
    {
        private const string UserId = "8bcd2782-e8cb-4643-8803-951d715fc202";
        private const string CalculationId = "3abc2782-e8cb-4643-8803-951d715fci23";
        private const string Username = "test-user";
        private const string SpecificationId = "spec-id-1";
        private const string JobId = "job-id";
        private const string CalculationName = "calc-name-1";
        private const string FundingStreamId = "fs-1";
        private const string DefaultSourceCode = "return 0";
        private const string Description = "test description";
        private const string CorrelationId = "4abc2782-e8cb-4643-8803-951d715fci29";

        private static CalculationService CreateCalculationService(
            ICalculationsRepository calculationsRepository = null,
            ILogger logger = null,
            ISearchRepository<CalculationIndex> searchRepository = null,
            IBuildProjectsService buildProjectsService = null,
            IPoliciesApiClient policiesApiClient = null,
            ICacheProvider cacheProvider = null,
            ICalcsResiliencePolicies resiliencePolicies = null,
            IVersionRepository<CalculationVersion> calculationVersionRepository = null,
            IJobManagement jobManagement = null,
            ISourceCodeService sourceCodeService = null,
            IFeatureToggle featureToggle = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = null,
            IValidator<CalculationCreateModel> calculationCreateModelValidator = null,
            IValidator<CalculationEditModel> calculationEditModelValidator = null,
            ISpecificationsApiClient specificationsApiClient = null,
            IGraphRepository graphRepository = null,
            ICalculationsFeatureFlag calculationsFeatureFlag = null,
            ICodeContextCache codeContextCache = null,
            ISourceFileRepository sourceFileRepository = null)
        {
            CalculationNameInUseCheck calculationNameInUseCheck = new CalculationNameInUseCheck(calculationsRepository ?? CreateCalculationsRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies());

            InstructionAllocationJobCreation instructionAllocationJobCreation = 
                new InstructionAllocationJobCreation(
                    calculationsRepository ?? CreateCalculationsRepository(),
                    resiliencePolicies ?? CalcsResilienceTestHelper.GenerateTestPolicies(),
                    logger ?? CreateLogger(),
                    calculationsFeatureFlag ?? CreateCalculationsFeatureFlag(),
                    jobManagement ?? CreateJobManagement(),
                    sourceFileRepository ?? Substitute.For<ISourceFileRepository>());

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
                    instructionAllocationJobCreation),
                graphRepository?? CreateGraphRepository(),
                CreateJobManagement(),
                codeContextCache ?? Substitute.For<ICodeContextCache>());
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

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
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
            });

            return resultsConfig.CreateMapper();
        }

        private static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }

        private static IGraphRepository CreateGraphRepository()
        {
            return Substitute.For<IGraphRepository>();
        }

        private static ICalculationsFeatureFlag CreateCalculationsFeatureFlag(bool graphEnabled = false)
        {
            ICalculationsFeatureFlag calculationsFeatureFlag = Substitute.For<ICalculationsFeatureFlag>();

            calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(graphEnabled);

            return calculationsFeatureFlag;
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

        private static DatasetDefinition CreateDatasetDefinitionWithTwoTableDefinitions()
        {
            return new DatasetDefinition
            {
                Id = "12345",
                Name = "14/15",
                TableDefinitions = new List<TableDefinition>
                {
                    new TableDefinition
                    {
                        Id = "1111",
                        Name = "Test Table Def 1",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                Id = "FD111",
                                Name = "APPupilPremium3YO",
                                Description = "Test description 1",
                                Type = FieldType.String,
                                IdentifierFieldType = IdentifierFieldType.LACode
                            },
                            new FieldDefinition
                            {
                                Id = "FD222",
                                Name = "Test field name 2",
                                Description = "Test description 2",
                                Type = FieldType.String,
                                IdentifierFieldType = null
                            },
                        }
                    },
                    new TableDefinition
                    {
                        Id = "2222",
                        Name = "Test Table Def 2",
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition
                            {
                                Id = "FD333",
                                Name = "Test field name 3",
                                Description = "Test description 3",
                                Type = FieldType.String,
                                IdentifierFieldType = IdentifierFieldType.LACode
                            },
                            new FieldDefinition
                            {
                                Id = "FD444",
                                Name = "Test field name 4",
                                Description = "Test description 4",
                                Type = FieldType.String,
                                IdentifierFieldType = null
                            },
                        }
                    }
                }
            };
        }
        private static Reference CreateAuthor()
        {
            return new Reference(UserId, Username);
        }
    }
}
