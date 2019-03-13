using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
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
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

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
                .Error("No specification Id was provided to UpdateAllocationLineResultStatus");
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenNoUpdateModelProvided_ReturnsBadRequest()
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
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null updateStatusModel was provided");

            logger
                .Received(1)
                .Error("Null updateStatusModel was provided to UpdateAllocationLineResultStatus");
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenUpdateModelWithNoProviders_ReturnsBadRequest()
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
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

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
                .Error("Null or empty providers was provided to UpdateAllocationLineResultStatus");
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenBatchingButNoUpdateModel_ReturnsBadRequest()
        {
            //arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
           {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-1",
                    AllocationLineIds = new[] { "AAAAA" }
                },
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111-2",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Approved
            };

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

            Job newJob = new Job { Id = "new-job-id" };

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(newJob);

            ICacheProvider cacheProvider = CreateCacheProvider();

            PublishedResultsService resultsService = CreateResultsService(logger, jobsApiClient: jobsApiClient, cacheProvider: cacheProvider);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeAssignableTo<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"New job: '{JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob}' created with id: '{newJob.Id}'"));

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<UpdatePublishedAllocationLineResultStatusModel>(Arg.Any<string>(), Arg.Any<UpdatePublishedAllocationLineResultStatusModel>());

            await
                jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(m =>
                        !string.IsNullOrWhiteSpace(m.InvokerUserDisplayName) &&
                        !string.IsNullOrWhiteSpace(m.InvokerUserId) &&
                        m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob &&
                        m.SpecificationId == specificationId &&
                        m.Properties["specification-id"] == specificationId &&
                        !string.IsNullOrWhiteSpace(m.Properties["cache-key"]) &&
                        m.Trigger.EntityId == specificationId &&
                        m.Trigger.EntityType == "Specification" &&
                        m.Trigger.Message == $"Updating allocation line results status"
                    ));
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenBatching_CreatesNewJob()
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
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null updateStatusModel was provided");

            logger
                .Received(1)
                .Error("Null updateStatusModel was provided to UpdateAllocationLineResultStatus");
        }
    }
}
