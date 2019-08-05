using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using NSubstitute;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Calcs.Interfaces;
using System.Linq;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCalculationsMetadataForSpecification_GivenNoSpecificationId_ReturnsBadRequestObjectResult()
        {
            //Arrange
            string specificationId = null;

            ILogger logger = CreateLogger();

            CalculationService calculationService = CreateCalculationService(logger: logger);

            //Act
            IActionResult actionResult = await calculationService.GetCalculationsMetadataForSpecification(specificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");

            logger
                .Received(1)
                .Error(Arg.Is($"No specificationId was provided to {nameof(calculationService.GetCalculationsMetadataForSpecification)}"));
        }

        [TestMethod]
        public async Task GetCalculationsMetadataForSpecification_GivenItemsFoundInCache_ReturnsItemsFromCache()
        {
            //Arrange
            List<CalculationMetadata> calculations = new List<CalculationMetadata>
            {
                new CalculationMetadata(),
                new CalculationMetadata()
            };

            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{SpecificationId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationMetadata>>(Arg.Is(cacheKey))
                .Returns(calculations);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            CalculationService calculationService = CreateCalculationService(
                cacheProvider: cacheProvider,
                calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await calculationService.GetCalculationsMetadataForSpecification(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeSameAs(calculations);

            await
                calculationsRepository
                    .DidNotReceive()
                    .GetCalculationsMetatadataBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetCalculationsMetadataForSpecification_GivenItemsNotFoundInCacheAndNotInDatabase_ReturnsEmptyList()
        {
            //Arrange
            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{SpecificationId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationMetadata>>(Arg.Is(cacheKey))
                .Returns((List<CalculationMetadata>)null);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(SpecificationId))
                .Returns((IEnumerable<CalculationMetadata>)null);

            CalculationService calculationService = CreateCalculationService(
                cacheProvider: cacheProvider,
                calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await calculationService.GetCalculationsMetadataForSpecification(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeSameAs(Enumerable.Empty<CalculationMetadata>());

            await
                cacheProvider
                    .DidNotReceive()
                    .SetAsync(Arg.Any<string>(), Arg.Any<List<CalculationMetadata>>());
        }

        [TestMethod]
        public async Task GetCalculationsMetadataForSpecification_GivenItemsNotFoundInCacheButFoundInDatabase_ReturnsItems()
        {
            //Arrange
            List<CalculationMetadata> calculations = new List<CalculationMetadata>
            {
                new CalculationMetadata(),
                new CalculationMetadata()
            };

            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{SpecificationId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationMetadata>>(Arg.Is(cacheKey))
                .Returns((List<CalculationMetadata>)null);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            CalculationService calculationService = CreateCalculationService(
                cacheProvider: cacheProvider,
                calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await calculationService.GetCalculationsMetadataForSpecification(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeSameAs(calculations);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Is(cacheKey), Arg.Any<List<CalculationMetadata>>());
        }
    }
}
