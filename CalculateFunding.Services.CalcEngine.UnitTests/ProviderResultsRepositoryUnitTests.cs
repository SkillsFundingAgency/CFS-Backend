using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using CalculationResult = CalculateFunding.Models.Calcs.CalculationResult;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using FundingLineResult = CalculateFunding.Models.Calcs.FundingLineResult;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;
using ProviderSummary = CalculateFunding.Models.ProviderLegacy.ProviderSummary;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class ProviderResultsRepositoryUnitTests
    {
        private IResultsApiClient _resultsApiClient;
        private Reference _user;
        private string _correlationId;

        [TestInitialize]
        public void SetUp()
        {
            _resultsApiClient = Substitute.For<IResultsApiClient>();
            _user = new Reference("test-user-id", "test-user-name");
            _correlationId = Guid.NewGuid().ToString();
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Name = "Specification 1",
                FundingPeriod = new Reference()
            };

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
                            Value = 1112.3M
                        }
                    },
                    FundingLineResults = new List<FundingLineResult>
                    {
                        new FundingLineResult
                        {
                            FundingLine = new Reference { Id = "fl1", Name = "funding line one" },
                            FundingLineFundingStreamId = "FS1",
                            Value = 112.3M
                        }
                    },
                    Id = Guid.NewGuid().ToString(),
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Name = "Provider 1",
                        ProviderType = "TYpe 1",
                        ProviderSubType = "Sub type 1",
                        Authority = "Authority",
                        UKPRN = "ukprn123",
                        URN = "urn123",
                        EstablishmentNumber = "en123",
                        UPIN = "upin123",
                        DateOpened = DateTime.Now
                    },
                    SpecificationId = "spec1"
                }
            };

            // Act
            await repo.SaveProviderResults(results, specificationSummary, 1, 1, _user, _correlationId);

            // Assert
            await cosmosRepository.Received(1).UpsertAsync<ProviderResult>(
                Arg.Any<ProviderResult>(),
                Arg.Any<string>(),
                Arg.Is<bool>(false),
                Arg.Is<bool>(false));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = NewRandomString(),
                Name = NewRandomString(),
                FundingPeriod = new Reference
                {
                    Id = NewRandomString()
                },
                LastEditedDate = new RandomDateTime()
            };

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = CalculationType.Template,
                            Value = null
                        }
                    },
                    FundingLineResults = new List<FundingLineResult>
                    {
                        new FundingLineResult
                        {
                            FundingLine = new Reference { Id = "fl1", Name = "funding line one" },
                            FundingLineFundingStreamId = "FS1",
                            Value = 112.3M
                        }
                    },
                    Id = Guid.NewGuid().ToString(),
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Name = "Provider 1",
                        ProviderType = "TYpe 1",
                        ProviderSubType = "Sub type 1",
                        Authority = "Authority",
                        UKPRN = "ukprn123",
                        URN = "urn123",
                        EstablishmentNumber = "en123",
                        UPIN = "upin123",
                        DateOpened = DateTime.Now
                    },
                    SpecificationId = "spec1"
                }
            };

            // Act
            await repo.SaveProviderResults(results, specificationSummary, 1, 1, _user, _correlationId);

            // Assert
            await cosmosRepository.Received(1).UpsertAsync<ProviderResult>(
                Arg.Any<ProviderResult>(),
                Arg.Any<string>(),
                Arg.Is<bool>(false),
                Arg.Is<bool>(false));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsButResultsNotChanged_ThenResultsNotSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            IProviderResultCalculationsHashProvider hashProvider = Substitute.For<IProviderResultCalculationsHashProvider>();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, calculationsHashProvider: hashProvider);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = NewRandomString(),
                Name = NewRandomString(),
                FundingPeriod = new Reference
                {
                    Id = NewRandomString()
                },
                LastEditedDate = new RandomDateTime()
            };

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = CalculationType.Template,
                            Value = null
                        }
                    },
                    Id = Guid.NewGuid().ToString(),
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Name = "Provider 1",
                        ProviderType = "TYpe 1",
                        ProviderSubType = "Sub type 1",
                        Authority = "Authority",
                        UKPRN = "ukprn123",
                        URN = "urn123",
                        EstablishmentNumber = "en123",
                        UPIN = "upin123",
                        DateOpened = DateTime.Now
                    },
                    SpecificationId = "spec1"
                }
            };

            // Act
            await repo.SaveProviderResults(results, specificationSummary, 1, 1, _user, _correlationId);

            // Assert
            await cosmosRepository.Received(0).BulkUpsertAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Is<bool>(false));
        }

        private static RandomString NewRandomString() => new RandomString();

        [TestMethod]
        public async Task SaveProviderResults_WhenResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenQueueSearchIndexWriterJob()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            IJobManagement jobManagement = CreateJobManagement();

            ProviderResultsRepository repo = CreateProviderResultsRepository(
                cosmosRepository,
                jobManagement: jobManagement);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = NewRandomString(),
                Name = NewRandomString(),
                FundingPeriod = new Reference
                {
                    Id = NewRandomString()
                },
                LastEditedDate = new RandomDateTime(),
                FundingStreams = new[]
                {
                    new Reference
                    {
                        Id = NewRandomString()
                    },
                    new Reference
                    {
                        Id = NewRandomString()
                    },
                    new Reference
                    {
                        Id = NewRandomString()
                    }
                }
            };

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = CalculationType.Template,
                            Value = 1112.3M
                        }
                    },
                    FundingLineResults = new List<FundingLineResult>
                    {
                        new FundingLineResult
                        {
                            FundingLine = new Reference { Id = "fl1", Name = "funding line one" },
                            FundingLineFundingStreamId = "FS1",
                            Value = 112.3M
                        }
                    },
                    Id = Guid.NewGuid().ToString(),
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Name = "Provider 1",
                        ProviderType = "TYpe 1",
                        ProviderSubType = "Sub type 1",
                        Authority = "Authority",
                        UKPRN = "ukprn123",
                        URN = "urn123",
                        EstablishmentNumber = "en123",
                        UPIN = "upin123",
                        DateOpened = DateTime.Now
                    },
                    SpecificationId = "spec1"
                }
            };

            // Act
            await repo.SaveProviderResults(results, specificationSummary, 1, 1, _user, _correlationId);

            // Assert
            await jobManagement.Received(1).QueueJob(Arg.Is<JobCreateModel>(_ =>
             _.JobDefinitionId == JobConstants.DefinitionNames.SearchIndexWriterJob &&
             _.Properties["specification-id"] == specificationSummary.GetSpecificationId() &&
             _.Properties["specification-name"] == specificationSummary.Name &&
             _.Properties["index-writer-type"] == SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter &&
             _.MessageBody == JsonConvert.SerializeObject(results.Select(x => x.Provider.Id))));

            await _resultsApiClient.Received(1)
                .QueueMergeSpecificationInformationJob(Arg.Is<MergeSpecificationInformationRequest>(_ =>
                    _.SpecificationInformation.Id == specificationSummary.Id &&
                    _.SpecificationInformation.Name == specificationSummary.Name &&
                    _.SpecificationInformation.LastEditDate == specificationSummary.LastEditedDate &&
                    _.SpecificationInformation.FundingStreamIds.SequenceEqual(specificationSummary.FundingStreams.Select(fs => fs.Id).ToArray()) &&
                    _.SpecificationInformation.FundingPeriodId == specificationSummary.FundingPeriod.Id &&
                    _.ProviderIds.SequenceEqual(new[] { "prov1" })));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenResultsSavedToCosmosSavesNull()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            IJobManagement jobManagement = CreateJobManagement();

            ProviderResultsRepository repo = CreateProviderResultsRepository(
                cosmosRepository,
                jobManagement: jobManagement);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Name = "Specification 1",
                FundingPeriod = new Reference()
            };

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
                            Value = null
                        }
                    },
                    FundingLineResults = new List<FundingLineResult>
                    {
                        new FundingLineResult
                        {
                            FundingLine = new Reference { Id = "fl1", Name = "funding line one" },
                            FundingLineFundingStreamId = "FS1",
                            Value = null
                        }
                    },
                    Id = Guid.NewGuid().ToString(),
                    Provider = new ProviderSummary
                    {
                        Id = "prov1",
                        Name = "Provider 1",
                        ProviderType = "TYpe 1",
                        ProviderSubType = "Sub type 1",
                        Authority = "Authority",
                        UKPRN = "ukprn123",
                        URN = "urn123",
                        EstablishmentNumber = "en123",
                        UPIN = "upin123",
                        DateOpened = DateTime.Now
                    },
                    SpecificationId = "spec1"
                }
            };

            // Act
            await repo.SaveProviderResults(results, specificationSummary, 1, 1, _user, _correlationId);

            // Assert
            await cosmosRepository.Received(1).UpsertAsync<ProviderResult>(
                Arg.Any<ProviderResult>(),
                Arg.Any<string>(),
                Arg.Is<bool>(false),
                Arg.Is<bool>(false));

            await jobManagement.Received(1).QueueJob(Arg.Is<JobCreateModel>(_ =>
            _.JobDefinitionId == JobConstants.DefinitionNames.SearchIndexWriterJob &&
            _.Properties["specification-id"] == specificationSummary.GetSpecificationId() &&
            _.Properties["specification-name"] == specificationSummary.Name &&
            _.Properties["index-writer-type"] == SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter &&
            _.MessageBody == JsonConvert.SerializeObject(results.Select(x => x.Provider.Id))));
        }

        private ProviderResultsRepository CreateProviderResultsRepository(
            ICosmosRepository cosmosRepository = null,
            ILogger logger = null,
            IProviderResultCalculationsHashProvider calculationsHashProvider = null,
            ICalculatorResiliencePolicies calculatorResiliencePolicies = null,
            IJobManagement jobManagement = null) =>
            new ProviderResultsRepository(
                cosmosRepository ?? CreateCosmosRepository(),
                logger ?? CreateLogger(),

                calculationsHashProvider ?? CreateCalcHashProvider(),
                calculatorResiliencePolicies ?? CreateCalculatorResiliencePolicies(),
                _resultsApiClient,
                jobManagement ?? CreateJobManagement());

        private static ICalculatorResiliencePolicies CreateCalculatorResiliencePolicies()
        {
            return CalcEngineResilienceTestHelper.GenerateTestPolicies();
        }

        private static IProviderResultCalculationsHashProvider CreateCalcHashProvider()
        {
            IProviderResultCalculationsHashProvider calculationsHashProvider = Substitute.For<IProviderResultCalculationsHashProvider>();

            calculationsHashProvider.TryUpdateCalculationResultHash(Arg.Any<ProviderResult>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(true);

            return calculationsHashProvider;
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICosmosRepository CreateCosmosRepository()
        {
            return Substitute.For<ICosmosRepository>();
        }

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        private static EngineSettings CreateEngineSettings()
        {
            return new EngineSettings();
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsNewProviderCalculationResultsIndexEnabled()
                .Returns(false);

            return featureToggle;
        }

        private static ISearchRepository<ProviderCalculationResultsIndex> CreateProviderCalculationResultsSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
        }
    }
}
