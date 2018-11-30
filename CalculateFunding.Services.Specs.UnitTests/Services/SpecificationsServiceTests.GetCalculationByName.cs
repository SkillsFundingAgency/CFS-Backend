using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Newtonsoft.Json;
using System.IO;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

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
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

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
            Calculation calc = new Calculation { Name = CalculationName };

            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns(calc);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for specification id {SpecificationId} and name {CalculationName}"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns((Calculation)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

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
