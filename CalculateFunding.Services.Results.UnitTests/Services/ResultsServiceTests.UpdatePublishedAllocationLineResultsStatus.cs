using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
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
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
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

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.First().FundingStreamResult.AllocationLineResult.Current as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Is("1111"))
                .Returns(newVersion);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            ResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

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

            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.First().Key == newVersion.ProviderId && m.First().Value.Version == 2));
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

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

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

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

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
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenAllResultsAreHeldAndAttemptToApproved_ReturnsOKObjectEnsureHistoryAdded()
        {
            //arrange
            string specificationId = "spec-1";
            string providerId = "1111";

            IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers = new[]
            {
                new UpdatePublishedAllocationLineResultStatusProviderModel
                {
                    ProviderId = providerId,
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

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();

            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion.Version = 2;
            newVersion.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>())
                .Returns(newVersion);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            ResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Arrange
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            await searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                        m.First().ProviderId == providerId &&
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
                        m.First().AllocationVersionNumber == 2 &&
                        m.First().AllocationStatus == "Approved" &&
                        m.First().AllocationAmount == (double)50.0 &&
                        m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                        m.First().ProviderName == "test provider name 1" &&
                        m.First().LaCode == "77777" &&
                        m.First().Authority == "London" &&
                        m.First().ProviderType == "test type" &&
                        m.First().SubProviderType == "test sub type" &&
                        m.First().EstablishmentNumber == "es123"
            ));

            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 1 && m.First().Key == newVersion.ProviderId && m.First().Value == newVersion));
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

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;


            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;


            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>())
                .Returns(newVersion1, newVersion2, newVersion3);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            ResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

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

            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 3));
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenThreeProvidersToApprove_ReturnsOKObjectResult()
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

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>())
                .Returns(newVersion1, newVersion2, newVersion3);

            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            ResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                publishedProviderResultsVersionRepository: versionRepository);

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

            await
               versionRepository
                   .Received(1)
                   .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>>>(m => m.Count() == 3));
        }

        [TestMethod]
        public async Task UpdatePublishedAllocationLineResultsStatus_GivenThreeProvidersToApprove_RequestsProviderProfileInformation()
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

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                publishedProviderResult.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            PublishedAllocationLineResultVersion newVersion1 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion1.Version = 2;
            newVersion1.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion2 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion2.Version = 2;
            newVersion2.Status = AllocationLineStatus.Approved;

            PublishedAllocationLineResultVersion newVersion3 = publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            newVersion3.Version = 2;
            newVersion3.Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<PublishedAllocationLineResultVersion>())
                .Returns(newVersion1, newVersion2, newVersion3);


            IPublishedProviderResultsRepository resultsProviderRepository = CreatePublishedProviderResultsRepository();
            resultsProviderRepository
                .GetPublishedProviderResultsForSpecificationIdAndProviderId(Arg.Is(specificationId), Arg.Any<IEnumerable<string>>())
                .Returns(publishedProviderResults);

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IMessengerService messengerService = CreateMessengerService();

            ResultsService resultsService = CreateResultsService(
                publishedProviderResultsRepository: resultsProviderRepository,
                specificationsRepository: specificationsRepository,
                messengerService: messengerService,
                publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.UpdatePublishedAllocationLineResultsStatus(request);

            //Assertl
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            await messengerService.Received(1).SendToQueue(Arg.Is(ServiceBusConstants.QueueNames.FetchProviderProfile), Arg.Any<IEnumerable<FetchProviderProfilingMessageItem>>(), Arg.Any<Dictionary<string, string>>());
        }
    }
}
