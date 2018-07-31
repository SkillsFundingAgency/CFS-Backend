using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger);

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

            ResultsService resultsService = CreateResultsService(logger);

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

            ResultsService resultsService = CreateResultsService(logger);

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
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenNoPublishResultsReturns_ReturnsNotFound()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1234"
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers
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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<PublishedProviderResult>)null);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No provider results to update for specification id: {specificationId}");
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenPublishResultsReturnsButUpdatingThrowsException_ReturnsInternalServerError()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(CreatePublishedProviderResults());
            resultsProviderRepository
                .When(x => x.SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>()))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenPublishResultsReturnsButNoAllocationLinesSpecified_ReturnsOKObjectResult()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(CreatePublishedProviderResults());

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(0);

            value
               .UpdatedProviderIds
               .Should()
               .Be(0);
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenPublishResultsReturnsAllocationLinesSpecifiedOnlyOneStatusChanged_ReturnsOKObjectResult()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();
            publishedProviderResults
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = new[]
                {
                    new PublishedAllocationLineResultVersion()
                }
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(1);

            value
               .UpdatedProviderIds
               .Should()
               .Be(1);
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenAllResultsAreHeldAndAttemptToPublish_ReturnsOKObjectResultWithZeroCounts()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Published
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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResults();

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = new[]
                {
                    new PublishedAllocationLineResultVersion()
                }
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(0);

            value
               .UpdatedProviderIds
               .Should()
               .Be(0);
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenResultsWithNullProviderInformation_ReturnsOKObjectResultWithZeroCounts()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

            UpdatePublishedAllocationLineResultStatusModel model = new UpdatePublishedAllocationLineResultStatusModel
            {
                Providers = Providers,
                Status = AllocationLineStatus.Published
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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = new[]
            {
                new PublishedProviderResult()
            };

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = new[]
                {
                    new PublishedAllocationLineResultVersion()
                }
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(0);

            value
               .UpdatedProviderIds
               .Should()
               .Be(0);
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenAllResultsAreHeldAndAttemptToPublish_ReturnsOKObjectEnsureHistoryAdded()
        {
            //arrange
            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = "1111",
                    AllocationLineIds = new[] { "AAAAA" }
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository, allocationNotificationFeedSearchRepository: searchRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            history
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            await searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                        m.First().ProviderId == "1111" &&
                        m.First().Title == "Allocation test allocation line 1 was Approved" &&
                        m.First().Summary == "test summary 1" &&
                        m.First().DatePublished.HasValue == false &&
                        m.First().FundingStreamId == "fs-1" &&
                        m.First().FundingStreamName == "funding stream 1" &&
                        m.First().FundingPeriodId == "Ay12345" &&
                        m.First().ProviderUkPrn == "1111" &&
                        m.First().ProviderUpin == "2222" &&
                        m.First().ProviderOpenDate.HasValue &&
                        m.First().AllocationLineId == "AAAAA" &&
                        m.First().AllocationLineName == "test allocation line 1" &&
                        m.First().AllocationVersionNumber == 1 &&
                        m.First().AllocationStatus == "Approved" &&
                        m.First().AllocationAmount == (double)50.0 &&
                        m.First().ProviderProfiling == "[]"
            ));
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenThreeProvidersToPublish_ReturnsOKObjectResultCreatesThreeHistoryItems()
        {
            //arrange
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

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            PublishedAllocationLineResultHistory history1 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history2 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-1",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history3 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-2",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history1);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-1"), Arg.Is("AAAAA"))
                .Returns(history2);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-2"), Arg.Is("AAAAA"))
                .Returns(history3);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(1);

            value
               .UpdatedProviderIds
               .Should()
               .Be(3);

            history1
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            history2
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            history3
                 .History
                 .Count()
                 .Should()
                 .Be(1);
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenThreeProvidersToApproveButNoProfilingReturned_ReturnsOKObjectResultCreatesThreeHistoryItemsLogsError()
        {
            //arrange
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

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            PublishedAllocationLineResultHistory history1 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history2 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-1",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history3 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-2",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history1);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-1"), Arg.Is("AAAAA"))
                .Returns(history2);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-2"), Arg.Is("AAAAA"))
                .Returns(history3);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: resultsProviderRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(1);

            value
               .UpdatedProviderIds
               .Should()
               .Be(3);

            history1
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            history2
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            history3
                 .History
                 .Count()
                 .Should()
                 .Be(1);

            logger
                .Received(1)
                .Error(Arg.Is("Failed to obtain profiling periods for provider: 1111 and period: fp-1"));
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenThreeProvidersToApproveButAnfdProfilingReturned_ReturnsOKObjectResult()
        {
            //arrange
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

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
            });

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

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IEnumerable<PublishedProviderResult> publishedProviderResults = CreatePublishedProviderResultsWithDifferentProviders();

            PublishedAllocationLineResultHistory history1 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history2 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-1",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            PublishedAllocationLineResultHistory history3 = new PublishedAllocationLineResultHistory
            {
                SpecificationId = specificationId,
                ProviderId = "1111-2",
                AllocationLine = new Models.Reference
                {
                    Id = "AAAAA"
                },
                History = null
            };

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationId(Arg.Is(specificationId))
                .Returns(publishedProviderResults);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111"), Arg.Is("AAAAA"))
                .Returns(history1);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-1"), Arg.Is("AAAAA"))
                .Returns(history2);

            resultsProviderRepository
                .GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Is("1111-2"), Arg.Is("AAAAA"))
                .Returns(history3);

            ProviderProfilingResponseModel providerProfilingResponse = new ProviderProfilingResponseModel
            {
                ProfilePeriods = new[]
                {
                    new ProfilingPeriod(),
                    new ProfilingPeriod()
                }
            };

            IProviderProfilingRepository providerProfilingRepository = CreateProfilingRepository();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(providerProfilingResponse);

            ResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: resultsProviderRepository, providerProfilingRepository: providerProfilingRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Assertl
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            UpdateAllocationResultsStatusCounts value = okObjectResult.Value as UpdateAllocationResultsStatusCounts;

            value
                .UpdatedAllocationLines
                .Should()
                .Be(1);

            value
               .UpdatedProviderIds
               .Should()
               .Be(3);
        }
    }
}
