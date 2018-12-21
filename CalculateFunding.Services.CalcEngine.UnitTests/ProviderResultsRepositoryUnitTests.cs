using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calculator.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class ProviderResultsRepositoryUnitTests
    {
        [TestMethod]
        public async Task SaveProviderResults_WhenNoResults_ThenNoResultsSaved()
        {
            // Arrange
            ProviderResultsRepository repo = CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository);

            IEnumerable<ProviderResult> results = Enumerable.Empty<ProviderResult>();

            // Act
            await repo.SaveProviderResults(results);

            // Assert
            await cosmosRepository.DidNotReceive().BulkCreateAsync(Arg.Any<IEnumerable<KeyValuePair<string, ProviderResult>>>());
            await searchRepository.DidNotReceive().Index(Arg.Any<IList<CalculationProviderResultsIndex>>());
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToCosmos()
        {
            // Arrange
            ProviderResultsRepository repo = CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository);

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
                            CalculationSpecification = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
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
            await repo.SaveProviderResults(results);

            // Assert
            await cosmosRepository.Received().BulkCreateAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenResults_ThenResultsSavedToSearch()
        {
            // Arrange
            ProviderResultsRepository repo = CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository);

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
                            CalculationSpecification = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
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
            await repo.SaveProviderResults(results);

            // Assert
            await searchRepository.Received(1).Index(Arg.Is<IList<CalculationProviderResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(1).Index(Arg.Is<IList<CalculationProviderResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationSpecificationId == results.First().CalculationResults.First().CalculationSpecification.Id &&
                r.First().CalculationSpecificationName == results.First().CalculationResults.First().CalculationSpecification.Name &&
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
            ProviderResultsRepository repo = CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository);

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
                            CalculationSpecification = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
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
            await repo.SaveProviderResults(results);

            // Assert
            await cosmosRepository.Received().BulkCreateAsync(Arg.Is<IEnumerable<KeyValuePair<string, ProviderResult>>>(r => r.Count() == 1));
        }

        [TestMethod]
        public async Task SaveProviderResults_WhenExcludedResults_ThenResultsSavedToSearch()
        {
            // Arrange
            ProviderResultsRepository repo = CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository);

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
                            CalculationSpecification = new Reference { Id = "calc1", Name = "calculation one" },
                            CalculationType = Models.Calcs.CalculationType.Funding,
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
            await repo.SaveProviderResults(results);

            // Assert
            await searchRepository.Received(1).Index(Arg.Is<IList<CalculationProviderResultsIndex>>(r => r.Count() == 1));

            await searchRepository.Received(1).Index(Arg.Is<IList<CalculationProviderResultsIndex>>(r =>
                r.First().SpecificationId == results.First().SpecificationId &&
                r.First().SpecificationName == "Specification 1" &&
                r.First().CalculationSpecificationId == results.First().CalculationResults.First().CalculationSpecification.Id &&
                r.First().CalculationSpecificationName == results.First().CalculationResults.First().CalculationSpecification.Name &&
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

        private static ProviderResultsRepository CreateProviderResultsRepository(out ICosmosRepository cosmosRepository, out ISearchRepository<CalculationProviderResultsIndex> searchRepository, out ISpecificationsRepository specificationsRepository)
        {
            cosmosRepository = Substitute.For<ICosmosRepository>();
            searchRepository = Substitute.For<ISearchRepository<CalculationProviderResultsIndex>>();
            specificationsRepository = Substitute.For<ISpecificationsRepository>();
            ILogger logger = Substitute.For<ILogger>();

            return new ProviderResultsRepository(cosmosRepository, searchRepository, specificationsRepository, logger);
        }
    }
}
