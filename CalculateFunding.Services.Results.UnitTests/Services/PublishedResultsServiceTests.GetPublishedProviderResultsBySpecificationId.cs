using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.ResultModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task GetPublishedProviderResultsBySpecificationId_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsBySpecificationId(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specification Id provided");

            logger
                .Received(1)
                .Error("No specification Id was provided to GetPublishedProviderResultsBySpecificationId");
        }

        [TestMethod]
        public async Task GetPublishedProviderResultsBySpecificationId_GivenNoProviderResultsFound_ReturnsEmptyList()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsBySpecificationId(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultsBySpecificationId_GivenProviderResultsFound_ReturnsProviderResults()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsBySpecificationId(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = okObjectResult.Value as IEnumerable<PublishedProviderResultModel>;

            publishedProviderResultModels
                .Count()
                .Should()
                .Be(1);

            publishedProviderResultModels
              .First()
              .ProviderName
              .Should()
              .Be("test provider name 1");

            publishedProviderResultModels
             .First()
             .ProviderId
             .Should()
             .Be("1111");

            publishedProviderResultModels
                .First()
                .FundingStreamResults
                .Count()
                .Should()
                .Be(2);

            publishedProviderResultModels
                .First()
                .FundingStreamResults
                .First()
                .AllocationLineResults
                .Count()
                .Should()
                .Be(2);

            publishedProviderResultModels
               .First()
               .FundingStreamResults
               .Last()
               .AllocationLineResults
               .Count()
               .Should()
               .Be(1);

        }
    }
}
