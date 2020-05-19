using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
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
        public void SelectSpecificationForFunding_GivenNoFundingPeriodOnSpecification_ThrowsException()
        {
            ILogger logger = CreateLogger();
            Specification specification = CreateSpecification();
            specification.Current.FundingPeriod = null;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(SpecificationId)
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            Func<Task<IActionResult>> invocation = () => service.SelectSpecificationForFunding(SpecificationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenNoFundingStreamAlreadySelectedInPeriod_ReturnsConflict()
        {
            Specification specification = CreateSpecification();
            Specification specificationWithFundingStreamClash = CreateSpecification();

            SpecificationVersion currentVersionOfSpecification = specification.Current;

            string commonFundingStreamId = currentVersionOfSpecification.FundingStreams.First().Id;

            specificationWithFundingStreamClash.Current.FundingStreams.First().Id = commonFundingStreamId;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(SpecificationId)
                .Returns(specification);
            specificationsRepository
                .GetSpecificationsSelectedForFundingByPeriod(currentVersionOfSpecification.FundingPeriod.Id)
                .Returns(new[] { specificationWithFundingStreamClash });

            IActionResult result = await CreateService(
                logs: CreateLogger(),
                specificationsRepository: specificationsRepository)
                .SelectSpecificationForFunding(SpecificationId);

            result
                .Should()
                .BeOfType<ConflictResult>();
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenNoSpecificationId_ReturnsBadRequestObject()
        {
            //Arrange
            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specification Id provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No specification Id was provided to SelectSpecificationForFunding"));
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenSpecificationCouldNotBeFound_ReturnsNotFoundObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Specification not found for id: {SpecificationId}");

            logger
                .Received(1)
                .Warning(Arg.Is($"Specification not found for id: {SpecificationId}"));
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenSpecificationFoundButAlreadySelected_ReturnsNoContentResult()
        {
            //Arrange
            Specification specification = CreateSpecification();
            specification.IsSelectedForFunding = true;


            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Attempt to mark specification with id: {SpecificationId} selected when already selected"));
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenSpecificationButUpdatingCosmosReturnsBadRequest_ReturnsInternalServerError()
        {
            //Arrange
            Specification specification = CreateSpecification();

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
            IActionResult result = await service.SelectSpecificationForFunding(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to set IsSelectedForFunding on specification for id: {SpecificationId} with status code: BadRequest");

            logger
                .Received(1)
                .Error($"Failed to set IsSelectedForFunding on specification for id: {SpecificationId} with status code: BadRequest");
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenSpecificationButUpdatingSearchFails_ReturnsInternalServerError()
        {
            // Arrange
            Specification specification = CreateSpecification();

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IEnumerable<IndexError> errors = new[]
            {
                new IndexError{ ErrorMessage = "failed" }
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(errors);

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.SelectSpecificationForFunding(SpecificationId);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to index search for specification {SpecificationId} with the following errors: failed");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to index search for specification {SpecificationId} with the following errors: failed"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to index search for specification {SpecificationId} with the following errors: failed"));

            await cacheProvider
                .Received(1)
                .RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenValidSpecification_ReturnsNoContentResult()
        {
            // Arrange
            Specification specification = CreateSpecification();

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.SelectSpecificationForFunding(SpecificationId);

            // Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await cacheProvider
                .Received(1)
                .RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");
        }
    }
}
