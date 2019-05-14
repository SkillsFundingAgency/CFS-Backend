using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void IsCalculationNameValid_WhenNoCalculationName_ThenThrowsArugmentNullException()
        {
            // Arrange
            CalculationService service = CreateCalculationService();

            // Act
            Func<Task> action = async () => await service.IsCalculationNameValid("spec1", null, null);

            // Assert
            action
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("calculationName");
        }

        [TestMethod]
        public void IsCalculationNameValid_WhenNoSpecificationId_ThenThrowsArugmentNullException()
        {
            // Arrange
            CalculationService service = CreateCalculationService();

            // Act
            Func<Task> action = async () => await service.IsCalculationNameValid(null, "calc1", null);

            // Assert
            action
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenSpecificationDoesNotExist_ThenReturnsNotFoundResult()
        {
            // Arrange
            string specificationId = "spec1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns((SpecificationSummary)null);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, "calc1", null);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenCalculationDoesNotExist_ThenReturnsOkResult()
        {
            // Arrange
            string specificationId = "spec1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(new List<Models.Calcs.Calculation>());

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, "calc1", null);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenCalculationDoesExist_ThenReturnsConflictResult()
        {
            // Arrange
            string specificationId = "spec1";
            string calcName = "calc1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            List<Models.Calcs.Calculation> existingCalcs = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Name = calcName,
                    SourceCodeName = calcName,
                    Id = "calc-id-1",
                    CalculationSpecification = new Common.Models.Reference { Id = "calc-spec-id" }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, null);

            // Assert
            result
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenCalculationDiffersByCase_ThenReturnsConflictResult()
        {
            // Arrange
            string specificationId = "spec1";
            string calcName = "calc1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            List<Models.Calcs.Calculation> existingCalcs = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Name = calcName.ToUpperInvariant(),
                    SourceCodeName = calcName.ToUpperInvariant(),
                    Id = "calc-id-1",
                    CalculationSpecification = new Common.Models.Reference { Id = "calc-spec-1" }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, null);

            // Assert
            result
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenCalculationDiffersBySpecialCharacter_ThenReturnsConflictResult()
        {
            // Arrange
            string specificationId = "spec1";
            string calcName = "calc1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            List<Models.Calcs.Calculation> existingCalcs = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Name = "calc+1",
                    SourceCodeName = "calc1",
                    Id = "calc-id-1",
                    CalculationSpecification = new Common.Models.Reference { Id = "calc-spec-1" }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, null);

            // Assert
            result
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenCalculationDiffersBySpace_ThenReturnsConflictResult()
        {
            // Arrange
            string specificationId = "spec1";
            string calcName = "calc1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            List<Models.Calcs.Calculation> existingCalcs = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Name = "calc 1",
                    SourceCodeName = "calc1",
                    Id = "calc-id-1",
                    CalculationSpecification = new Common.Models.Reference { Id = "calc-spec-1" }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, null);

            // Assert
            result
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task IsCalculationNameValid_WhenSameCalculation_ThenReturnsOkResult()
        {
            // Arrange
            string specificationId = "spec1";
            string calcName = "calc1";
            string calcSpecId = "calc-spec-id-1";

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new SpecificationSummary { Id = specificationId });

            List<Models.Calcs.Calculation> existingCalcs = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Name = calcName,
                    SourceCodeName = calcName,
                    Id = "calc-1",
                    CalculationSpecification = new Common.Models.Reference { Id = calcSpecId }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, calcSpecId);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();
        }
    }
}
