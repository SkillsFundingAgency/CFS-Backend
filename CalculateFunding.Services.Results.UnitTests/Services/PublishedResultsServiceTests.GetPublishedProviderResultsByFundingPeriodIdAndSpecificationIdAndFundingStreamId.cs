using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.ResultModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenNoSpecificationIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger);

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

            PublishedResultsService resultsService = CreateResultsService(logger);

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

            PublishedResultsService resultsService = CreateResultsService(logger);

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

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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

            IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults = CreatePublishedProviderResultByAllocationLineViewModel();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultsSummaryByFundingPeriodIdAndSpecificationIdAndFundingStreamId(Arg.Is(fundingPeriodId), Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(publishedProviderResults);

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

            //Assert
            OkObjectResult okObjectResult = actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = okObjectResult.Value
                .Should()
                .BeAssignableTo<IEnumerable<PublishedProviderResultModel>>()
                .Subject;

            publishedProviderResultModels
                .Should()
                .HaveCount(1, "there should be 1 results");

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
                .Should()
                .HaveCount(2, "there should be 2 funding stream results");

            publishedProviderResultModels
                .First()
                .FundingStreamResults
                .First(f => f.FundingStreamId == "fs-1")
                .AllocationLineResults
                .Should()
                .HaveCountLessOrEqualTo(2, "there should be 2 allocation line results for fs-1");

            publishedProviderResultModels
               .First()
               .FundingStreamResults
               .First(f => f.FundingStreamId == "fs-2")
               .AllocationLineResults
               .Should()
               .HaveCountLessOrEqualTo(2, "there should be 1 allocation line results for fs-2");
        }

        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenProviderResultsFound_ReturnsProviderResultsOrderedByProviderName()
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

            IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults = CreatePublishedProviderResultByAllocationLineViewModelWithDifferentProvidersUnordered();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultsSummaryByFundingPeriodIdAndSpecificationIdAndFundingStreamId(Arg.Is(fundingPeriodId), Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(publishedProviderResults);

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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
                .Be(3);

            IEnumerable<string> providerNames = publishedProviderResultModels.Select(r => r.ProviderName);
            providerNames.Should().BeInAscendingOrder();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId_GivenProviderResultsFound_ReturnsAllocationResultsOrderedByAllocationName()
        {
            // Arrange
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

            IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults = CreatePublishedProviderResultByAllocationLineViewModelWithMultipleAllocationLines();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultsSummaryByFundingPeriodIdAndSpecificationIdAndFundingStreamId(Arg.Is(fundingPeriodId), Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(publishedProviderResults);

            PublishedResultsService resultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            // Act
            IActionResult actionResult = await resultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(request);

            // Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = okObjectResult.Value as IEnumerable<PublishedProviderResultModel>;

            publishedProviderResultModels
                .Count()
                .Should()
                .Be(1);

            IEnumerable<string> allocationResultNames = publishedProviderResultModels.First().FundingStreamResults.First().AllocationLineResults.Select(r => r.AllocationLineName);
            allocationResultNames.Should().BeInAscendingOrder();
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResultsWithDifferentProvidersUnordered()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "zz test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-1",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-1",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-1",
                                    Name = "aa test provider name 2"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-2",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                             AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-2",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-2",
                                    Name = "gg test provider name 3"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                }
            };
        }
        static IEnumerable<PublishedProviderResultByAllocationLineViewModel> CreatePublishedProviderResultByAllocationLineViewModelWithDifferentProvidersUnordered()
        {
            return new[]
            {
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId = "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 50,
                    VersionNumber = "0.1",
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "zz test provider name 1",
                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-1",
                    FundingStreamId = "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 100,
                    VersionNumber = "0.1",
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111-1",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "aa test provider name 2",

                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111-2",
                    FundingStreamId = "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 100,
                    VersionNumber = "0.1",
                    Ukprn = "1111-2",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "gg test provider name 3"
                }
            };
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResultsWithMultipleAllocationLines()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "zz test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA-2",
                                Name = "aa test allocation line 2",
                                ShortName = "tal2",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new PublishedFundingStreamDefinition
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PublishedPeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                             AllocationLine = new PublishedAllocationLineDefinition
                            {
                                Id = "AAAAA",
                                Name = "gg test allocation line 3",
                                ShortName = "tal3",
                                FundingRoute = PublishedFundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                }
            };
        }
        static IEnumerable<PublishedProviderResultByAllocationLineViewModel> CreatePublishedProviderResultByAllocationLineViewModelWithMultipleAllocationLines()
        {
            return new[]
            {
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId= "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "AAAAA",
                    AllocationLineName = "zz test allocation line 1",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 50,
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",
                    VersionNumber = "0.1"

                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId= "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "BBBBB",
                    AllocationLineName = "zz test allocation line 2",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 50,
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",
                    VersionNumber = "0.1"
                },
                new PublishedProviderResultByAllocationLineViewModel
                {
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamId= "fs-1",
                    FundingStreamName = "funding stream 1",
                    AllocationLineId = "CCCCC",
                    AllocationLineName = "zz test allocation line 3",
                    Status = AllocationLineStatus.Held,
                    FundingAmount = 50,
                    LastUpdated = DateTimeOffset.Now,
                    Ukprn = "1111",
                    Authority = "London",
                    ProviderType = "test type",
                    ProviderName = "test provider name 1",
                    VersionNumber = "0.1"
                }
            };
        }
    }
}
