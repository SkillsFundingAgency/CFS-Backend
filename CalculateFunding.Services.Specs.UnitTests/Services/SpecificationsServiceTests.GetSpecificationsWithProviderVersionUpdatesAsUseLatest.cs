using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetSpecificationsWithProviderVersionUpdatesAsUseLatest_GivenSpecificationWasNotFound_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns((Enumerable.Empty<Specification>()));

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Specifications was not found with CoreProviderVersionUpdates as UseLatest"));
        }

        [TestMethod]
        public async Task GetSpecificationsWithProviderVersionUpdatesAsUseLatest_GivenSpecificationWasFound_ReturnsSpecification()
        {
            //Arrange

            SpecificationVersion sv1 = new SpecificationVersion { SpecificationId = "spec1", FundingPeriod = new Reference { Id = "18/19", Name = "18/19" }, CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest };

            Specification spec1 = new Specification { Id = "spec1", IsSelectedForFunding = true, Current = sv1 };

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns(new[] { spec1 });

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<SpecificationSummary> summaries = okObjectResult.Value as IEnumerable<SpecificationSummary>;

            summaries
               .Count()
               .Should()
               .Be(1);
        }

    }
}
