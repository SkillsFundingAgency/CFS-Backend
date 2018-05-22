using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using System.Linq;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationWasNotFound_ReturnsNotFoundt()
        {
            //Arrange
            IEnumerable<Specification> specs = Enumerable.Empty<Specification>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification was not found for name: {SpecificationName}"));
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = new[]
            {
                new Specification()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            Specification objContent = (Specification)((OkObjectResult)result).Value;

            objContent
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification found for name: {SpecificationName}"));
        }

    }
}
