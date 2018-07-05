using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenNoSpecificationId_ReturnsBadRequestObject()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(request);

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
            IActionResult result = await service.SelectSpecificationForFunding(request);

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
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Specification specification = CreateSpecification();
            specification.IsSelectedForFunding = true;


            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"Attempt to mark specification with id: {SpecificationId} selected when alreday selected"));
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenSpecificationButUpdatingCosmosReturnsBadRequest_ReturnsInternalServerError()
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
            IActionResult result = await service.SelectSpecificationForFunding(request);

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
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

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

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(request);

            //Assert
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
        }

        [TestMethod]
        public async Task SelectSpecificationForFunding_GivenPublishingResultsfails_ReturnsInternalServerError()
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

            Specification specification = CreateSpecification();

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(Enumerable.Empty<IndexError>());

            IResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .PublishProviderResults(Arg.Is(SpecificationId))
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, 
                searchRepository: searchRepository, resultsRepository: resultsRepository);

            //Act
            IActionResult result = await service.SelectSpecificationForFunding(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to publish provider results for specification id: {SpecificationId} with status code: BadRequest");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to publish provider results for specification id: {SpecificationId} with status code: BadRequest"));


            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to publish provider results for specification id: {SpecificationId} with status code: BadRequest"));
        }
    }
}
