using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task UpdateCacheWithCalculationStarted_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult actionResult = await service.UpdateCalculationLastUpdatedDate(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specification id was provided");

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to UpdateCalculationLastUpdatedDate"));
        }

        [TestMethod]
        public async Task UpdateCacheWithCalculationStarted_GivenSpecificationIdButDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
               .GetSpecificationById(Arg.Is(SpecificationId))
               .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult actionResult = await service.UpdateCalculationLastUpdatedDate(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A specification for id {SpecificationId} could not found"));
        }

        [TestMethod]
        public async Task UpdateCacheWithCalculationStarted_GivenSpecificationButUpdatingFails_ReturnsInternalServerError()
        {
            //Arrange
            Specification specification = new Specification
            {
                Id = SpecificationId
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
               .GetSpecificationById(Arg.Is(SpecificationId))
               .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult actionResult = await service.UpdateCalculationLastUpdatedDate(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to update calculation last updated date on specification {SpecificationId}");

            logger
                .Received(1)
                .Error($"Failed to update calculation last updated date on specification {SpecificationId}");
        }

        [TestMethod]
        public async Task UpdateCacheWithCalculationStarted_GivenSpecificationButUpdatingAndUpdateSucceeeds_ReturnsNoContentRemovesFromCache()
        {
            //Arrange
            Specification specification = new Specification
            {
                Id = SpecificationId
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
               .GetSpecificationById(Arg.Is(SpecificationId))
               .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult actionResult = await service.UpdateCalculationLastUpdatedDate(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<NoContentResult>();

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"));

        }
    }
}