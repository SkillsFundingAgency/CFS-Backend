using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null, null));

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(new List<Calculation>());

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            List<Calculation> existingCalcs = new List<Calculation>
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        Name = calcName,
                        SourceCodeName = calcName
                    },
                    Id = "calc-id-1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            List<Calculation> existingCalcs = new List<Calculation>
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        Name = calcName.ToUpperInvariant(),
                        SourceCodeName = calcName.ToUpperInvariant()
                    },
                    Id = "calc-id-1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            List<Calculation> existingCalcs = new List<Calculation>
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        Name =  "calc+1",
                        SourceCodeName = "calc1"
                    },
                    Id = "calc-id-1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            List<Calculation> existingCalcs = new List<Calculation>
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        Name =  "calc 1",
                        SourceCodeName = "calc1"
                    },
                    Id = "calc-id-1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

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
            string calcSpecId = "calc-1";

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, new SpecModel.SpecificationSummary { Id = specificationId }));

            List<Calculation> existingCalcs = new List<Calculation>
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        Name = calcName,
                        SourceCodeName = calcName
                    },
                   
                    Id = "calc-1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(existingCalcs);

            CalculationService service = CreateCalculationService(specificationsApiClient: specificationsApiClient, calculationsRepository: calculationsRepository);

            // Act
            IActionResult result = await service.IsCalculationNameValid(specificationId, calcName, calcSpecId);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();
        }
    }
}
