using CalculateFunding.Services.Core.Interfaces.Caching;
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
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenSpecificationIdsAreNull_ReturnsBadRequestObjectResult()
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
            IActionResult result = await service.CheckCalculationProgressForSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("There were no specifications found");

            logger
                .Received(1)
                .Error(Arg.Is("There were no specifications found"));
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
            IActionResult result = await service.CheckCalculationProgressForSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CheckCalculationProgressForSpecifications_WhenCacheisOk_ReturnOkObjectResult()
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

            cacheProvider.GetAsync<SpecificationCalculationProgress>($"calculationProgress-{SpecificationId}").Returns(new SpecificationCalculationProgress(SpecificationId,5,SpecificationCalculationProgress.CalculationProgressStatus.Started));

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.CheckCalculationProgressForSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }
    }
}
