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
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

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
                .Error("No specification Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");
        }

        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenNoFundingStreamIdProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "fundingPeriodId" , new StringValues(fundingPeriodId)},
             });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty fundingStream Id provided");

            logger
                .Received(1)
                .Error("No fundingStream Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");
        }


        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingPeriodId_GivenNoFundingPeriodIdProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "fundingStreamId", new StringValues(fundingStreamId) },
             });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty fundingPeriod Id provided");

            logger
                .Received(1)
                .Error("No fundingPeriod Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");
        }


        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenNoProviderResultsFound_ReturnsEmptyList()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "fundingPeriodId" , new StringValues(fundingPeriodId) },
                { "fundingStreamId", new StringValues(fundingStreamId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();
        }


        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenProviderResultsFound_ReturnsProviderResults()

        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "fundingPeriodId" , new StringValues(fundingPeriodId)},
                { "fundingStreamId", new StringValues(fundingStreamId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(Arg.Is(fundingPeriodId),Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

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
