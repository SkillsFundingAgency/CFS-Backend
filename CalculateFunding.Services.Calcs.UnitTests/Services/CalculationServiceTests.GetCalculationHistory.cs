using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces;
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
        async public Task GetCalculationHistory_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationHistory(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationHistory"));
        }

        [TestMethod]
        async public Task GetCalculationHistory_GivenCalculationIdWasProvidedButHistoryWasNull_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IVersionRepository<CalculationVersion> versionsRepository = CreateCalculationVersionRepository();
            versionsRepository
                .GetVersions(Arg.Is(CalculationId))
                .Returns((IEnumerable<CalculationVersion>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationVersionRepository: versionsRepository);

            //Act
            IActionResult result = await service.GetCalculationHistory(CalculationId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationHistory_GivenCalculationIdWasProvided_ReturnsOK()
        {
            //Arrange
            IEnumerable<CalculationVersion> versions = new List<CalculationVersion>
            {
                new CalculationVersion(),
                new CalculationVersion()
            };

            ILogger logger = CreateLogger();

            IVersionRepository<CalculationVersion> versionsRepository = CreateCalculationVersionRepository();
            versionsRepository
                .GetVersions(Arg.Is(CalculationId))
                .Returns(versions);

            CalculationService service = CreateCalculationService(logger: logger, calculationVersionRepository: versionsRepository);

            //Act
            IActionResult result = await service.GetCalculationHistory(CalculationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }
    }
}
