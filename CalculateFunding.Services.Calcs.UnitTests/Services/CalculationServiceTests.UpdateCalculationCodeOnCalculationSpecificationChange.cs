using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation To Update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
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
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return OriginalName()",
                DecimalPlaces = 6,
            });


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
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenCalculationsFoundReferencingCalculationToBeUpdatedHasDifferentNameCasing_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
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
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return OriginalName()",
                DecimalPlaces = 6,
            });

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
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationSpecificationChange_WhenNoCalculationsFoundReferencingCalculationToBeUpdated_ThenNoCalculationsUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, specificationRepository: specificationRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            Models.Specs.CalculationVersionComparisonModel comparison = new Models.Specs.CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Calculation to update",
                    CalculationType = CalculationType.Funding,
                },
                Previous = new Models.Specs.Calculation()
                {
                    Id = "calcSpec1",
                    Name = "Original Name",
                    CalculationType = CalculationType.Funding,
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
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc1.Save(new CalculationVersion()
            {
                SourceCode = "Return 10",
                DecimalPlaces = 6,
            });

            calculations.Add(calc1);

            Calculation calc2 = new Calculation()
            {
                Id = "referenceCalc",
                Name = "Calling Calculation To Update",
                SpecificationId = specificationId,
                FundingPeriod = new Reference("fp1", "Funding Period"),
                History = new List<CalculationVersion>(),
                CalculationSpecification = new Reference("calcSpec1", "Calculation to Update"),
                CalculationType = CalculationType.Funding,
                Description = "Calculation Description",
                BuildProjectId = "bpC1",
                Policies = new List<Reference>(),
            };
            calc2.Save(new CalculationVersion()
            {
                SourceCode = "Return 50",
                DecimalPlaces = 6,
            });


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

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationSpecificationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(0);
        }
    }
}
