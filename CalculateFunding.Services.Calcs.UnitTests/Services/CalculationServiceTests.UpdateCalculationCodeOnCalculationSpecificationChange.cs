using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenCalculationsFoundReferencingCalculationToBeUpdated_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository, calculationVersionRepository: versionRepository, buildProjectsService: buildProjectsService);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation To Update",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return 10",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return OriginalName()",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "Return CalculationToUpdate()",
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            BuildProject buildProject = new BuildProject
            {
                Build = new Build
                {
                    SourceFiles = new List<SourceFile>()
                }
            };

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            updatedCalculations
                .First()
                .Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");

            updatedCalculations
                .First()
                .Current.Version
                .Should()
                .Be(2);

            updatedCalculations
                .First()
                .Id
                .Should()
                .Be("referenceCalc");

            await buildProjectsService
                .DidNotReceive()
                .GetBuildProjectForSpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenCalculationsFoundReferencingCalculationToBeUpdatedHasDifferentNameCasing_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository, calculationVersionRepository: versionRepository, buildProjectsService: buildProjectsService);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return 10",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return OriginalName()",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "Return CalculationToUpdate()",
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            updatedCalculations
                .First()
                .Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");

            updatedCalculations
                .First()
                .Current.Version
                .Should()
                .Be(2);

            updatedCalculations
                .First()
                .Id
                .Should()
                .Be("referenceCalc");

            await buildProjectsService
                .DidNotReceive()
                .GetBuildProjectForSpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenNoCalculationsFoundReferencingCalculationToBeUpdated_ThenNoCalculationsUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository, calculationVersionRepository: versionRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = Models.Specs.CalculationType.Funding,
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>();
            Calculation calc1 = new Calculation()
            {
                Id = calculationId,
                Name = "Calculation to Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return 10",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
                Current = new CalculationVersion()
                {
                    SourceCode = "Return 50",
                    DecimalPlaces = 6,
                }
            };

            calculations.Add(calc2);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "Return CalculationToUpdate()",
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(0);
        }
    }
}
