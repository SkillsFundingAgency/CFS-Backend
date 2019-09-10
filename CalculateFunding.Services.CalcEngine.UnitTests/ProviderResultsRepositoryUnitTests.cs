using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    [TestClass]
    public class ProviderResultsRepositoryUnitTests
    {
        [TestMethod]
        public async Task SaveProviderResults_WhenNoResults_ThenNoResultsSaved()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository);

            IEnumerable<ProviderResult> results = Enumerable.Empty<ProviderResult>();

            // Act
            await repo.SaveProviderResults(results, 1, 1);

            // Assert
            await cosmosRepository.DidNotReceive().BulkCreateAsync(Arg.Any<IEnumerable<KeyValuePair<string, ProviderResult>>>());
            await searchRepository.DidNotReceive().Index(Arg.Any<IList<CalculationProviderResultsIndex>>());
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

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
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToSearch()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
                            Value = 1112.3M
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
            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationName == results.First().CalculationResults.First().Calculation.Name &&
                r.First().CalculationId == results.First().CalculationResults.First().Calculation.Id &&
                r.First().CalculationType == results.First().CalculationResults.First().CalculationType.ToString() &&
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
                r.First().CalculationResult == Convert.ToDouble(results.First().CalculationResults.First().Value) &&
                r.First().IsExcluded == false));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
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
        }
        
         [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsButResultsNotChanged_ThenResultsNotSavedToCosmos()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IProviderResultCalculationsHashProvider hashProvider = Substitute.For<IProviderResultCalculationsHashProvider>();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository, calculationsHashProvider: hashProvider);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
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


        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResults_ThenResultsSavedToSearch()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
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
            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(1).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationName == results.First().CalculationResults.First().Calculation.Name &&
                r.First().CalculationId == results.First().CalculationResults.First().Calculation.Id &&
                r.First().CalculationType == results.First().CalculationResults.First().CalculationType.ToString() &&
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
                r.First().CalculationResult == null &&
                r.First().IsExcluded == true));
        }
        
        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsButResultsNotChanged_ThenResultsNotSavedToSearch()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IProviderResultCalculationsHashProvider hashProvider = Substitute.For<IProviderResultCalculationsHashProvider>();

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository, calculationsHashProvider: hashProvider);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
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
            await searchRepository.Received(0).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(0).Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationName == results.First().CalculationResults.First().Calculation.Name &&
                r.First().CalculationId == results.First().CalculationResults.First().Calculation.Id &&
                r.First().CalculationType == results.First().CalculationResults.First().CalculationType.ToString() &&
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
                r.First().CalculationResult == null &&
                r.First().IsExcluded == true));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenResultsSavedToSearch()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateProviderCalculationResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsNewProviderCalculationResultsIndexEnabled()
                .Returns(true);

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsRepository: specificationsRepository, providerCalculationResultsSearchRepository: searchRepository, featureToggle: featureToggle);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
                            Value = 1112.3M
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
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationId.Any() &&
                r.First().CalculationId.First() == results.First().CalculationResults.First().Calculation.Id &&
                r.First().CalculationName.Any() &&
                r.First().CalculationName.First() == results.First().CalculationResults.First().Calculation.Name &&
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
                r.First().CalculationResult.First() == results.First().CalculationResults.First().Value.ToString()));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResultsAndIsNewProviderCalculationResultsIndexEnabled_ThenResultsSavedToCosmosSavesNull()
        {
            // Arrange
            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateProviderCalculationResultsSearchRepository();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsNewProviderCalculationResultsIndexEnabled()
                .Returns(true);

            ProviderResultsRepository repo = CreateProviderResultsRepository(cosmosRepository, specificationsRepository: specificationsRepository, providerCalculationResultsSearchRepository: searchRepository, featureToggle: featureToggle);

            specificationsRepository.GetSpecificationSummaryById(Arg.Any<string>()).Returns(new SpecificationSummary { Name = "Specification 1" });

            IEnumerable<ProviderResult> results = new List<ProviderResult>
            {
                new ProviderResult
                {
                    AllocationLineResults = new List<AllocationLineResult>
                    {
                        new AllocationLineResult
                        {
                            AllocationLine = new Reference{ Id = "alloc 1", Name = "Allocation one" },
                            Value = 1112.3M
                        }
                    },
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            AllocationLine = new Reference { Id = "alloc1", Name = "Allocation one" },
                            Calculation = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Template,
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
               r.First().CalculationResult.First() == "null"));
        }

        private static ProviderResultsRepository CreateProviderResultsRepository(
            ICosmosRepository cosmosRepository = null,
            ISearchRepository<CalculationProviderResultsIndex> searchRepository = null,
            ISpecificationsRepository specificationsRepository = null,
            ILogger logger = null,
            IFeatureToggle featureToggle = null,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository = null,
            EngineSettings engineSettings = null,
            IProviderResultCalculationsHashProvider calculationsHashProvider = null)
        {

            return new ProviderResultsRepository(
                cosmosRepository ?? CreateCosmosRepository(),
                searchRepository ?? CreateCalculationProviderResultsSearchRepository(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                logger ?? CreateLogger(),
                providerCalculationResultsSearchRepository ?? CreateProviderCalculationResultsSearchRepository(),
                featureToggle ?? CreateFeatureToggle(),
                engineSettings ?? CreateEngineSettings(),
                calculationsHashProvider ?? CreateCalcHashProvider());
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

        private static ISearchRepository<CalculationProviderResultsIndex> CreateCalculationProviderResultsSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationProviderResultsIndex>>();
        }

        private static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
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
