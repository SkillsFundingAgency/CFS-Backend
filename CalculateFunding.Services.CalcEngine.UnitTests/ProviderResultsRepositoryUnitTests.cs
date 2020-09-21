using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using CalculationResult = CalculateFunding.Models.Calcs.CalculationResult;
using FundingLineResult = CalculateFunding.Models.Calcs.FundingLineResult;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;
using ProviderSummary = CalculateFunding.Models.ProviderLegacy.ProviderSummary;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class ProviderResultsRepositoryUnitTests
    {
        private IResultsApiClient _resultsApiClient;

        [TestInitialize]
        public void SetUp()
        {
            _resultsApiClient = Substitute.For<IResultsApiClient>();
        }
        
        [TestMethod]
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsApiClient);

            specificationsApiClient.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, 
                new SpecModel.SpecificationSummary
                {
                    Name = "Specification 1",
                    FundingPeriod = new Reference()
                }));

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
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await cosmosRepository.Received().BulkUpsertAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Is<bool>(false));
        }
        
        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsApiClient);

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
            
            specificationsApiClient.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await cosmosRepository.Received().BulkUpsertAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Is<bool>(false));
        }
        
         [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsButResultsNotChanged_ThenResultsNotSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            IProviderResultCalculationsHashProvider hashProvider = Substitute.For<IProviderResultCalculationsHashProvider>();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsApiClient, calculationsHashProvider: hashProvider);

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
            
            specificationsApiClient.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await cosmosRepository.Received(0).BulkUpsertAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Is<bool>(false));
        }

        private static RandomString NewRandomString() => new RandomString();

        [TestMethod]
        public async Task SaveProviderResults_WhenResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenResultsSavedToSearch()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateProviderCalculationResultsSearchRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsNewProviderCalculationResultsIndexEnabled()
                .Returns(true);

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsApiClient: specificationsApiClient, providerCalculationResultsSearchRepository: searchRepository, featureToggle: featureToggle);

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = NewRandomString(),
                Name = NewRandomString(),
                FundingPeriod = new Reference
                {
                    Id = NewRandomString()
                },
                LastEditedDate = new RandomDateTime(),
                FundingStreams = new []
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
            
            specificationsApiClient.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == specificationSummary.Name &&
                r.First().CalculationId.Any() &&
                r.First().CalculationId.First() == results.First().CalculationResults.First().Calculation.Id &&
                r.First().CalculationName.Any() &&
                r.First().CalculationName.First() == results.First().CalculationResults.First().Calculation.Name &&
                r.First().FundingLineId.Any() &&
                r.First().FundingLineId.First() == results.First().FundingLineResults.First().FundingLine.Id &&
                r.First().FundingLineName.Any() &&
                r.First().FundingLineName.First() == results.First().FundingLineResults.First().FundingLine.Name &&
                r.First().ProviderId == results.First().Provider.Id &&
                r.First().ProviderName == results.First().Provider.Name &&
                r.First().ProviderType == results.First().Provider.ProviderType &&
                r.First().ProviderSubType == results.First().Provider.ProviderSubType &&
                r.First().LocalAuthority == results.First().Provider.Authority &&
                r.First().UKPRN == results.First().Provider.UKPRN &&
                r.First().URN == results.First().Provider.URN &&
                r.First().UPIN == results.First().Provider.UPIN &&
                r.First().EstablishmentNumber == results.First().Provider.EstablishmentNumber &&
                r.First().OpenDate == results.First().Provider.DateOpened &&
                r.First().CalculationResult.Any() &&
                r.First().CalculationResult.First() == results.First().CalculationResults.First().Value.ToString() &&
                r.First().FundingLineResult.Any() &&
                r.First().FundingLineResult.First() == results.First().FundingLineResults.First().Value.ToString()));

            await _resultsApiClient.Received(1)
                .QueueMergeSpecificationInformationJob(Arg.Is<MergeSpecificationInformationRequest>(_ =>
                    _.SpecificationInformation.Id == specificationSummary.Id &&
                    _.SpecificationInformation.Name == specificationSummary.Name &&
                    _.SpecificationInformation.LastEditDate == specificationSummary.LastEditedDate &&
                    _.SpecificationInformation.FundingStreamIds.SequenceEqual(specificationSummary.FundingStreams.Select(fs => fs.Id).ToArray()) &&
                    _.SpecificationInformation.FundingPeriodId == specificationSummary.FundingPeriod.Id &&
                    _.ProviderIds.SequenceEqual(new [] { "prov1" })));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenResultsSavedToCosmosSavesNull()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateProviderCalculationResultsSearchRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsNewProviderCalculationResultsIndexEnabled()
                .Returns(true);

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsApiClient: specificationsApiClient, providerCalculationResultsSearchRepository: searchRepository, featureToggle: featureToggle);

            specificationsApiClient.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary
            {
                Name = "Specification 1",
                FundingPeriod = new Reference()
            }));

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
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await cosmosRepository.Received().BulkUpsertAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Is<bool>(false));

            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(r =>
               r.First().SpecificationId == results.First().SpecificationId &&
               r.First().SpecificationName == "Specification 1" &&
               r.First().CalculationId.Any() &&
               r.First().CalculationId.First() == results.First().CalculationResults.First().Calculation.Id &&
               r.First().CalculationName.Any() &&
               r.First().CalculationName.First() == results.First().CalculationResults.First().Calculation.Name &&
               r.First().FundingLineId.Any() &&
               r.First().FundingLineId.First() == results.First().FundingLineResults.First().FundingLine.Id &&
               r.First().FundingLineName.Any() &&
               r.First().FundingLineName.First() == results.First().FundingLineResults.First().FundingLine.Name &&
               r.First().ProviderId == results.First().Provider.Id &&
               r.First().ProviderName == results.First().Provider.Name &&
               r.First().ProviderType == results.First().Provider.ProviderType &&
               r.First().ProviderSubType == results.First().Provider.ProviderSubType &&
               r.First().LocalAuthority == results.First().Provider.Authority &&
               r.First().UKPRN == results.First().Provider.UKPRN &&
               r.First().URN == results.First().Provider.URN &&
               r.First().UPIN == results.First().Provider.UPIN &&
               r.First().EstablishmentNumber == results.First().Provider.EstablishmentNumber &&
               r.First().OpenDate == results.First().Provider.DateOpened &&
               r.First().CalculationResult.Any() &&
               r.First().CalculationResult.First() == "null" &&
               r.First().FundingLineResult.Any() &&
               r.First().FundingLineResult.First() == "null"));
        }

        private ProviderResultsRepository CreateProviderResultsRepository(
            ICosmosRepository cosmosRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            ILogger logger = null,
            IFeatureToggle featureToggle = null,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository = null,
            EngineSettings engineSettings = null,
            IProviderResultCalculationsHashProvider calculationsHashProvider = null,
            ICalculatorResiliencePolicies calculatorResiliencePolicies = null) =>
            new ProviderResultsRepository(
                cosmosRepository ?? CreateCosmosRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                logger ?? CreateLogger(),
                providerCalculationResultsSearchRepository ?? CreateProviderCalculationResultsSearchRepository(),
                featureToggle ?? CreateFeatureToggle(),
                engineSettings ?? CreateEngineSettings(),
                calculationsHashProvider ?? CreateCalcHashProvider(),
                calculatorResiliencePolicies ?? CreateCalculatorResiliencePolicies(),
                _resultsApiClient);

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

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
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
