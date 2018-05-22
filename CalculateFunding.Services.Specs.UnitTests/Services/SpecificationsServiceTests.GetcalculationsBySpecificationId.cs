using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetCalculationsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenNoCalculationsExist_ReturnsOK()
        {
            // Arrange
            IEnumerable<Calculation> calculations = Enumerable.Empty<Calculation>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Verbose(Arg.Is("Calculations were found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenCalculationsExist_ReturnsOK()
        {
            // Arrange
            List<Calculation> calculations = new List<Calculation>();
            Calculation calc = new Calculation()
            {
                Name = "Test Calc 1",
            };

            calculations.Add(calc);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations.AsEnumerable());

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should().BeAssignableTo<IEnumerable<Calculation>>()
                .Which.Should().HaveCount(calculations.Count);

            logger
                .Received(1)
                .Verbose(Arg.Is("Calculations were found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenNullResponseFromRepository_ReturnsNotFound()
        {
            // Arrange
            List<Calculation> calculations = null;

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeAssignableTo<string>()
                .Which.Should().Be("No calculations could be retrieved");

            logger
                .Received(1)
                .Error(Arg.Is("No calculations could be retrieved found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

    }
}
