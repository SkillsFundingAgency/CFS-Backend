using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public class PublishedProviderResultsAssemblerServiceTests
    {
        [TestMethod]
        public void AssemblePublishedProviderResults_GivenNoFundingPeriodFound_ThrowsException()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                FundingPeriod = new Reference("fp1", "funding period 1")
            };

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService();

            //Act
            Func<Task> test = async () => await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            test
                .ShouldThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to find a funding period for id: {specification.FundingPeriod.Id}");
        }

        [TestMethod]
        public void AssemblePublishedProviderResults_ButGetAllFundingStreamsReturnsNull_ThrowsException()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-1"
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(CreateFundingPeriod(new Reference("fp1", "funding period 1")));

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository: specificationsRepository);

            //Act
            Func<Task> test = async () => await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            test
                .ShouldThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Failed to get all funding streams");
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_ButUnableToFindFundingStreamForProvidedFundingStreamId_ThrowsException()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001"
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-1"
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(CreateFundingPeriod(new Reference("fp1", "funding period 1")));

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            ICacheProvider cacheProvider = CreateCacheProvider();

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            Func<Task> test = async () => await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            test
                .ShouldThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to find a funding stream for id: fs-1");

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams), Arg.Is<FundingStream[]>( m => m.First().Id == "fs-001"));
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_ButUnableToFindFundingStreamForProvidedFundingStreamIdWhenReturnedFromCache_ThrowsException()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001"
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-1"
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(CreateFundingPeriod(new Reference("fp1", "funding period 1")));

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(fundingStreams.ToArray());

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            Func<Task> test = async () => await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            test
                .ShouldThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to find a funding stream for id: fs-1");

            await
                specificationsRepository
                .DidNotReceive()
                .GetFundingStreams();
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_ButNollocationLineResultsFound_ReturnsEmptyProviderResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001"
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-001"
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(CreateFundingPeriod(new Reference("fp1", "funding period 1")));

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(fundingStreams.ToArray());

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_AndOneAllocationLineFoundForFundingStream__ReturnsOneProviderResult()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        }
                    }
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = "spec-id-1",
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-001",
                        Name = "fs one"
                    }
                }
            };

            FundingPeriod fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(fundingStreams.ToArray());

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(1);

            PublishedProviderResult result = results.First();

            result.Title.Should().Be("Allocation test allocation line 1 was Held");
            result.Summary.Should().Be("UKPRN: ukprn-001, version 1");
            result.Id.Should().Be("ukprn-001_spec-id-1_AAAAA");
            result.Provider.URN.Should().Be("urn");
            result.Provider.UKPRN.Should().Be("ukprn");
            result.Provider.UPIN.Should().Be("upin");
            result.Provider.EstablishmentNumber.Should().Be("12345");
            result.Provider.Authority.Should().Be("authority");
            result.Provider.ProviderType.Should().Be("prov type");
            result.Provider.ProviderSubType.Should().Be("prov sub type");
            result.Provider.Id.Should().Be("ukprn-001");
            result.Provider.Name.Should().Be("prov name");
            result.SpecificationId.Should().Be("spec-id-1");
            result.FundingStreamResult.FundingStream.Id.Should().Be("fs-001");
            result.FundingStreamResult.FundingStream.Name.Should().Be("fs one");
            result.FundingStreamResult.AllocationLineResult.AllocationLine.Id.Should().Be("AAAAA");
            result.FundingStreamResult.AllocationLineResult.AllocationLine.Name.Should().Be("test allocation line 1");
            result.FundingStreamResult.AllocationLineResult.Current.Should().NotBeNull();
            result.FundingStreamResult.AllocationLineResult.Current.Author.Id.Should().Be("authorId");
            result.FundingStreamResult.AllocationLineResult.Current.Author.Name.Should().Be("authorName");
            result.FundingPeriod.Should().BeSameAs(fundingPeriod);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_AndTwoAllocationLineFoundForFundingStream__EnsuresTwoResultsAssembled()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        },
                        new AllocationLine
                        {
                            Id = "BBBBB",
                            Name = "test allocation line 2"
                        }
                    }
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = "spec-id-1",
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-001",
                        Name = "fs one"
                    }
                }
            };

            FundingPeriod fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(fundingStreams.ToArray());

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WhenThreeAllocatioLinesProvidedButOnlyFindsTwoInResults__CreatesTwoAssembledResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResults();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        },
                        new AllocationLine
                        {
                            Id = "BBBBB",
                            Name = "test allocation line 2"
                        },
                        new AllocationLine
                        {
                            Id = "CCCCC",
                            Name = "test allocation line 3"
                        }
                    }
                }
            };

            Reference author = CreateAuthor();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = "spec-id-1",
                FundingPeriod = new Reference("fp1", "funding period 1"),
                FundingStreams = new[]
                {
                    new FundingStream
                    {
                        Id = "fs-001",
                        Name = "fs one"
                    }
                }
            };

            FundingPeriod fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(fundingStreams.ToArray());

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository, cacheProvider);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);
        }

        static PublishedProviderResultsAssemblerService CreateAssemblerService(
            ISpecificationsRepository specificationsRepository = null, ICacheProvider cacheProvider = null, ILogger logger = null)
        {
            return new PublishedProviderResultsAssemblerService(
                specificationsRepository ?? CreateSpecificationsRepository(), 
                cacheProvider ?? CreateCacheProvider(),
                logger ?? CreateLogger());
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IEnumerable<ProviderResult> CreateProviderResults()
        {
            IEnumerable<ProviderResult> results = new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = 123,
                            CalculationType = Models.Calcs.CalculationType.Funding
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
                        }
                    },
                AllocationLineResults = new List<AllocationLineResult>
                {
                    new AllocationLineResult
                    {
                        AllocationLine = new Reference
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1"
                        },
                        Value = 50
                    },
                    new AllocationLineResult
                    {
                        AllocationLine = new Reference
                        {
                            Id = "BBBBB",
                            Name = "test allocation line 2"
                        },
                        Value = 100
                    }
                },
                Provider = new ProviderSummary
                {
                    Id = "ukprn-001",
                    Name = "prov name",
                    ProviderType = "prov type",
                    ProviderSubType = "prov sub type",
                    Authority = "authority",
                    UKPRN = "ukprn",
                    UPIN = "upin",
                    URN = "urn",
                    EstablishmentNumber = "12345",
                    DateOpened = DateTime.Now.AddDays(-7),
                    ProviderProfileIdType = "UKPRN"
                }
            }
            };

            return results;
        }

        static Reference CreateAuthor()
        {
            return new Reference("authorId", "authorName");
        }

        static FundingPeriod CreateFundingPeriod(Reference reference)
        {
            return new FundingPeriod
            {
                Id = reference.Id,
                Name = reference.Name,
                StartDate = DateTimeOffset.Now.AddYears(-5),
                EndDate = DateTimeOffset.Now.AddYears(5),
                Type = "Any-Type"
            };
        }
    }
}
