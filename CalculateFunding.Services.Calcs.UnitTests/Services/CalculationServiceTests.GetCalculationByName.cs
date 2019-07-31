using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel();
            
            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(model);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainAcalculationName_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId
            };

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(model);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation name was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationExists_ReturnsSuccess()
        {
            //Arrange
            CalculationVersion calculationVersion = new CalculationVersion { Name = CalculationName };

            Calculation calc = new Calculation { Current = calculationVersion };

            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns(calc);

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, logger: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(model);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculationVersion);
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            Calculation calc = new Calculation { Current = new CalculationVersion { Name = CalculationName } };

            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository, logger: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(model);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for specification id {SpecificationId} and name {CalculationName}"));
        }
    }
}
