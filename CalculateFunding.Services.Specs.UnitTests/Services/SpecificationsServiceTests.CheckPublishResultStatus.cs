using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
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
        public async Task CheckCalculationProgressForSpecifications_WhenHttpRequestIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            HttpRequest request = null;

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("The request is null");

            logger
                .Received(1)
                .Error(Arg.Is("The http request is null"));
        }

        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenTheHttpRequestQueryIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            request.Query.Returns(x => null);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("the request query is empty or null");

            logger
                .Received(1)
                .Error(Arg.Is("The http request query is empty or null"));
        }

        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenQueryStringValuesAreEmpty_ReturnsBadRequestObjectResult()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {

            });
            request.Query.Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenCacheIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();

            request.Query.Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenCacheisOk_ReturnsOkObjectResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ICacheProvider cacheProvider = Substitute.For<ICacheProvider>();

            cacheProvider.GetAsync<SpecificationCalculationExecutionStatus>($"{CacheKeys.CalculationProgress}{SpecificationId}").Returns(new SpecificationCalculationExecutionStatus(SpecificationId, 5, CalculationProgressStatus.InProgress));

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }
        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenCacheisCorrupted_ReturnsBadRequestObjectResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ICacheProvider cacheProvider = Substitute.For<ICacheProvider>();

            cacheProvider.GetAsync<SpecificationCalculationExecutionStatus>($"{CacheKeys.CalculationProgress}{SpecificationId}").Returns<SpecificationCalculationExecutionStatus>(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.CheckPublishResultStatus(request);
            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }
    }
}
