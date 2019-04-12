using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

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
                .Error("No specification Id was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenNoUpdateModelProvided_ReturnsBadRequest()
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

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null filterCriteria was provided");

            logger
                .Received(1)
                .Error("Null filterCriteria was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }

        [TestMethod]
        public async Task GetConfirmationDetailsForApprovePublishProviderResults_GivenUpdateModelWithNoProviders_ReturnsBadRequest()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.GetConfirmationDetailsForApprovePublishProviderResults(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty providers was provided");

            logger
                .Received(1)
                .Error("Null or empty providers was provided to GetConfirmationDetailsForApprovePublishProviderResults");
        }
    }
}
