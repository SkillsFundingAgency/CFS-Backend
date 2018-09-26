using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

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
                .Should().ThrowExactly<Exception>()
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
                .Should()
                .ThrowExactly<Exception>()
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

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            Func<Task> test = async () => await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to find a funding stream for id: fs-1");

            await
                specificationsRepository
                .Received(1)
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

            specificationsRepository
                 .GetFundingStreams()
                 .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

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
                    Name = "fs one",
                    ShortName = "fs1",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1",
                            ShortName = "AA",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        }
                    },
                    PeriodType = new PeriodType{ Id = "AY", StartDay = 1, StartMonth = 8, EndDay = 31, EndMonth = 7, Name = "period-type" }
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
                        Name = "fs one",
                        PeriodType = new PeriodType
                        {
                            Id = "AY"
                        }
                    }
                }
            };

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);


            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

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
            result.Id.Should().NotBeEmpty();
            result.FundingStreamResult.AllocationLineResult.Current.Provider.URN.Should().Be("urn");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.UKPRN.Should().Be("ukprn");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.UPIN.Should().Be("upin");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.EstablishmentNumber.Should().Be("12345");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.Authority.Should().Be("authority");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType.Should().Be("prov type");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderSubType.Should().Be("prov sub type");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.Id.Should().Be("ukprn-001");
            result.FundingStreamResult.AllocationLineResult.Current.Provider.Name.Should().Be("prov name");
            result.SpecificationId.Should().Be("spec-id-1");
            result.FundingStreamResult.FundingStream.Id.Should().Be("fs-001");
            result.FundingStreamResult.FundingStream.Name.Should().Be("fs one");
            result.FundingStreamResult.FundingStream.ShortName.Should().Be("fs1");
            result.FundingStreamResult.FundingStream.PeriodType.Id.Should().Be("AY");
            result.FundingStreamResult.FundingStream.PeriodType.StartDay.Should().Be(1);
            result.FundingStreamResult.FundingStream.PeriodType.StartMonth.Should().Be(8);
            result.FundingStreamResult.FundingStream.PeriodType.EndDay.Should().Be(31);
            result.FundingStreamResult.FundingStream.PeriodType.EndMonth.Should().Be(7);
            result.FundingStreamResult.FundingStream.PeriodType.Name.Should().Be("period-type");
            result.FundingStreamResult.AllocationLineResult.AllocationLine.Id.Should().Be("AAAAA");
            result.FundingStreamResult.AllocationLineResult.AllocationLine.Name.Should().Be("test allocation line 1");
            result.FundingStreamResult.AllocationLineResult.Current.Should().NotBeNull();
            result.FundingStreamResult.AllocationLineResult.Current.Author.Id.Should().Be("authorId");
            result.FundingStreamResult.AllocationLineResult.Current.Author.Name.Should().Be("authorName");
            result.FundingStreamResult.AllocationLineResult.AllocationLine.FundingRoute.Should().Be(FundingRoute.LA);
            result.FundingStreamResult.AllocationLineResult.AllocationLine.IsContractRequired.Should().BeTrue();
            result.FundingPeriod.Should().BeSameAs(fundingPeriod);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WithSingleNullValuedFundingResult_ReturnsNoResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

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


            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    Name = "fs one",
                    ShortName = "fs1",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1",
                            ShortName = "AA",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        }
                    },
                    PeriodType = new PeriodType{ Id = "AY", StartDay = 1, StartMonth = 8, EndDay = 31, EndMonth = 7, Name = "period-type" }
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
                        Name = "fs one",
                        PeriodType = new PeriodType
                        {
                            Id = "AY"
                        }
                    }
                }
            };

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                            .GetFundingStreams()
                            .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WithMultipleNullValuedFundingResult_ReturnsNoResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                            Calculation = new Reference { Id = "calc-id-3", Name = "calc name 3" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-4", Name = "calc spec name 4"},
                            Calculation = new Reference { Id = "calc-id-4", Name = "calc name 4" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
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


            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    Name = "fs one",
                    ShortName = "fs1",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1",
                            ShortName = "AA",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        }
                    },
                    PeriodType = new PeriodType{ Id = "AY", StartDay = 1, StartMonth = 8, EndDay = 31, EndMonth = 7, Name = "period-type" }
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
                        Name = "fs one",
                        PeriodType = new PeriodType
                        {
                            Id = "AY"
                        }
                    }
                }
            };

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WithMultipleFundingCalculationsContainingNullsAndValues_ReturnsSumOfValues()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                            Calculation = new Reference { Id = "calc-id-3", Name = "calc name 3" },
                            Value = 25,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-4", Name = "calc spec name 4"},
                            Calculation = new Reference { Id = "calc-id-4", Name = "calc name 4" },
                            Value = 27,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
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


            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    Name = "fs one",
                    ShortName = "fs1",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1",
                            ShortName = "AA",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        }
                    },
                    PeriodType = new PeriodType{ Id = "AY", StartDay = 1, StartMonth = 8, EndDay = 31, EndMonth = 7, Name = "period-type" }
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
                        Name = "fs one",
                        PeriodType = new PeriodType
                        {
                            Id = "AY"
                        }
                    }
                }
            };

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(1);

            results
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .AllocationLine
                .Id
                .Should()
                .Be("AAAAA");

            results
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Value
                .Should()
                .Be(52);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WithMultipleFundingCalculationsContainingNullsAndValuesAndMultipleAllocationLines_ReturnsSumOfValuesForEachAllocationLine()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                            Calculation = new Reference { Id = "calc-id-3", Name = "calc name 3" },
                            Value = 12345,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("BBBBB", "test allocation line 2"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-4", Name = "calc spec name 4"},
                            Calculation = new Reference { Id = "calc-id-4", Name = "calc name 4" },
                            Value = 27,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-5", Name = "calc spec name 5"},
                            Calculation = new Reference { Id = "calc-id-5", Name = "calc name 5" },
                            Value = 27,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA", "test allocation line 1"),

                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-6", Name = "calc spec name 6"},
                            Calculation = new Reference { Id = "calc-id-6", Name = "calc name 6" },
                            Value = 79,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("BBBBB", "test allocation line 2"),

                        },
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


            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream
                {
                    Id = "fs-001",
                    Name = "fs one",
                    ShortName = "fs1",
                    AllocationLines = new List<AllocationLine>
                    {
                        new AllocationLine
                        {
                            Id = "AAAAA",
                            Name = "test allocation line 1",
                            ShortName = "AA",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        },
                        new AllocationLine
                        {
                            Id = "BBBBB",
                            Name = "test allocation line 2",
                            ShortName = "BB",
                            FundingRoute = FundingRoute.LA,
                            IsContractRequired = true
                        }
                    },
                    PeriodType = new PeriodType{ Id = "AY", StartDay = 1, StartMonth = 8, EndDay = 31, EndMonth = 7, Name = "period-type" }
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
                        Name = "fs one",
                        PeriodType = new PeriodType
                        {
                            Id = "AY"
                        }
                    }
                }
            };

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);

            results
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .AllocationLine
                .Id
                .Should()
                .Be("AAAAA");

            results
                .First()
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Value
                .Should()
                .Be(54);

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>(results);
            publishedProviderResults[1]
                .FundingStreamResult
                .AllocationLineResult
                .AllocationLine
                .Id
                .Should()
                .Be("BBBBB");

            publishedProviderResults[1]
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Value
                .Should()
                .Be(12424);

        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_AndTwoAllocationLineFoundForFundingStream__EnsuresTwoResultsAssembled()
        {
            IEnumerable<ProviderResult> providerResults = CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines();

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
                    },
                    PeriodType = new PeriodType{ Id = "AY" }
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

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);

            List<PublishedProviderResult> publishedProviderResults = results.ToList();

            PublishedAllocationLineResultVersion allocationLine1 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "AAAAA").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine1
            .Should()
            .NotBeNull();

            allocationLine1
                .Value
                .Should()
                .Be(123);

            PublishedAllocationLineResultVersion allocationLine2 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "BBBBB").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine2
            .Should()
            .NotBeNull();

            allocationLine2
                .Value
                .Should()
                .Be(10);

        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_WhenThreeAllocatioLinesProvidedButOnlyFindsTwoInResults_CreatesTwoAssembledResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines();

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
                    },
                    PeriodType = new PeriodType{ Id = "AY" }
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

            Period fundingPeriod = CreateFundingPeriod(new Reference("fp1", "funding period 1"));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriodById(Arg.Is("fp1"))
                .Returns(fundingPeriod);

            specificationsRepository
                 .GetFundingStreams()
                 .Returns(fundingStreams);

            PublishedProviderResultsAssemblerService assemblerService = CreateAssemblerService(specificationsRepository);

            //Act
            IEnumerable<PublishedProviderResult> results = await assemblerService.AssemblePublishedProviderResults(providerResults, author, specification);

            //Assert
            results
                .Count()
                .Should()
                .Be(2);
        }

        [TestMethod]
        public void AssemblePublishedCalculationResults_WhenCalculationResultsGeneratedIsProvidedWithNoItems_ThenEmptyResultsReturned()
        {
            // Arrange
            List<ProviderResult> providerResults = new List<ProviderResult>();
            Reference author = new Reference("a", "Author Name");
            SpecificationCurrentVersion specification = new SpecificationCurrentVersion();


            PublishedProviderResultsAssemblerService service = CreateAssemblerService();

            // Act
            IEnumerable<PublishedProviderCalculationResult> results = service.AssemblePublishedCalculationResults(providerResults, author, specification);

            // Assert
            results
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void AssemblePublishedCalculationResults_WhenCalculationResultsGenerated_ThenPublishedProviderCalculationResultsAreReturned()
        {
            // Arrange
            List<ProviderResult> providerResults = new List<ProviderResult>();
            Reference author = new Reference("a", "Author Name");
            SpecificationCurrentVersion specification = new SpecificationCurrentVersion();

            List<Policy> policies = new List<Policy>();
            policies.Add(new Policy()
            {
                Id = "p1",
                Name = "Policy 1",
                Calculations = new List<Calculation>(),
                SubPolicies = new List<Policy>(),
            });

            policies.Add(new Policy()
            {
                Id = "p2",
                Name = "Policy 2",
                Calculations = new List<Calculation>()
                {
                     new Calculation()
                     {
                         Id = "calc1",
                         Name = "Calculation 1 - Policy 2",
                         CalculationType = CalculationType.Funding,
                         AllocationLine = new Reference("al1", "Allocation Line 1"),
                     },
                     new Calculation()
                     {
                         Id = "calc2",
                         Name = "Calculation 2 - Policy 2",
                         CalculationType = CalculationType.Funding,
                         AllocationLine = new Reference("al2", "Allocation Line 2"),
                     },
                     new Calculation()
                     {
                         Id = "calc3",
                         Name = "Calculation 3 - Policy 2",
                         CalculationType = CalculationType.Number,
                         IsPublic = true,
                         AllocationLine = new Reference("al1", "Allocation Line 1"),
                     },
                },
                SubPolicies = new List<Policy>(),
            });

            specification.Policies = policies;

            providerResults.Add(new ProviderResult()
            {
                AllocationLineResults = new List<AllocationLineResult>(),
                CalculationResults = new List<CalculationResult>()
                {
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("calc1", "Calculation Specification 1"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 12345.66M,
                    }
                },
                Id = "p1",
                Provider = new ProviderSummary()
                {
                    Id = "provider1",
                    Name = "Provider 1",
                    UKPRN = "123"
                },
                SourceDatasets = new List<object>(),
                SpecificationId = specification.Id,
            });

            PublishedProviderResultsAssemblerService service = CreateAssemblerService();

            // Act
            IEnumerable<PublishedProviderCalculationResult> results = service.AssemblePublishedCalculationResults(providerResults, author, specification);

            // Assert
            results
                .Should()
                .HaveCount(1);

            PublishedProviderCalculationResult result = results.First();

            result.Approved.Should().BeNull();
            result.Current.Author.Should().BeEquivalentTo(author);
            result.Current.CalculationType.Should().Be(PublishedCalculationType.Funding);
            result.Current.Commment.Should().BeNullOrWhiteSpace();
            result.Current.Date.Should().BeAfter(DateTimeOffset.Now.AddHours(-1));
            result.Current.Provider.Should().BeEquivalentTo(new ProviderSummary()
            {
                Id = "provider1",
                Name = "Provider 1",
                UKPRN = "123",
            });
            result.Current.Value.Should().Be(12345.66M);
            result.Current.Version.Should().Be(1);
            result.Policy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
            result.ParentPolicy.Should().BeNull();
        }

        [TestMethod]
        public void AssemblePublishedCalculationResults_WhenCalculationResultsGeneratedForASubPolicy_ThenPublishedProviderCalculationResultsAreReturned()
        {
            // Arrange
            List<ProviderResult> providerResults = new List<ProviderResult>();
            Reference author = new Reference("a", "Author Name");
            SpecificationCurrentVersion specification = GenerateSpecificationWithPoliciesAndSubpolicies();

            providerResults.Add(new ProviderResult()
            {
                AllocationLineResults = new List<AllocationLineResult>(),
                CalculationResults = new List<CalculationResult>()
                {
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("subpolicy2Calc2", "Subpolicy 1 Calculation 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 12345.66M,
                    }
                },
                Id = "p1",
                Provider = new ProviderSummary()
                {
                    Id = "provider1",
                    Name = "Provider 1",
                    UKPRN = "123"
                },
                SourceDatasets = new List<object>(),
                SpecificationId = specification.Id,
            });

            PublishedProviderResultsAssemblerService service = CreateAssemblerService();

            // Act
            IEnumerable<PublishedProviderCalculationResult> results = service.AssemblePublishedCalculationResults(providerResults, author, specification);

            // Assert
            results
                .Should()
                .HaveCount(1);

            PublishedProviderCalculationResult result = results.First();

            result.Approved.Should().BeNull();
            result.Current.Author.Should().BeEquivalentTo(author);
            result.Current.CalculationType.Should().Be(PublishedCalculationType.Funding);
            result.Current.Commment.Should().BeNullOrWhiteSpace();
            result.Current.Date.Should().BeAfter(DateTimeOffset.Now.AddHours(-1));
            result.Current.Provider.Should().BeEquivalentTo(new ProviderSummary()
            {
                Id = "provider1",
                Name = "Provider 1",
                UKPRN = "123",
            });
            result.Current.Value.Should().Be(12345.66M);
            result.Current.Version.Should().Be(1);
            result.Policy.Should().BeEquivalentTo(new Reference("subpolicy2", "SubPolicy 2"));
            result.ParentPolicy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
        }

        [TestMethod]
        public void AssemblePublishedCalculationResults_WhenMultipleCalculationResultsGeneratedForPoliciesAndSubpolicies_ThenPublishedProviderCalculationResultsAreReturned()
        {
            // Arrange
            List<ProviderResult> providerResults = new List<ProviderResult>();
            Reference author = new Reference("a", "Author Name");
            SpecificationCurrentVersion specification = GenerateSpecificationWithPoliciesAndSubpolicies();

            providerResults.Add(new ProviderResult()
            {
                AllocationLineResults = new List<AllocationLineResult>(),
                CalculationResults = new List<CalculationResult>()
                {
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("subpolicy2Calc2", "Subpolicy 1 Calculation 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 12345.66M,
                    },
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("calc1", "Calculation 1 - Policy 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 12345.2M,
                    },
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("calc2", "Calculation 2 - Policy 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 12345.3M,
                    },
                },
                Id = "p1",
                Provider = new ProviderSummary()
                {
                    Id = "provider1",
                    Name = "Provider 1",
                    UKPRN = "123"
                },
                SourceDatasets = new List<object>(),
                SpecificationId = specification.Id,
            });

            providerResults.Add(new ProviderResult()
            {
                AllocationLineResults = new List<AllocationLineResult>(),
                CalculationResults = new List<CalculationResult>()
                {
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("subpolicy2Calc3", "Subpolicy 1 Calculation 3"),
                        CalculationType = Models.Calcs.CalculationType.Number,
                        Value = 2.1M,
                    },
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("calc1", "Calculation 1 - Policy 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 2.2M,
                    },
                    new CalculationResult()
                    {
                        CalculationSpecification = new Reference("calc2", "Calculation 2 - Policy 2"),
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        Value = 2.3M,
                    },
                },
                Id = "p2",
                Provider = new ProviderSummary()
                {
                    Id = "provider2",
                    Name = "Provider 1",
                    UKPRN = "456"
                },
                SourceDatasets = new List<object>(),
                SpecificationId = specification.Id,
            });

            PublishedProviderResultsAssemblerService service = CreateAssemblerService();

            // Act
            IEnumerable<PublishedProviderCalculationResult> results = service.AssemblePublishedCalculationResults(providerResults, author, specification);

            // Assert
            results
                .Should()
                .HaveCount(5);

            PublishedProviderCalculationResult result = results.First();

            List<PublishedProviderCalculationResult> resultsList = new List<PublishedProviderCalculationResult>(results);

            result.CalculationSpecification.Should().BeEquivalentTo(new Reference("subpolicy2Calc2", "Subpolicy 1 Calculation 2"));
            result.Approved.Should().BeNull();
            result.Current.Author.Should().BeEquivalentTo(author);
            result.Current.CalculationType.Should().Be(PublishedCalculationType.Funding);
            result.Current.Commment.Should().BeNullOrWhiteSpace();
            result.Current.Date.Should().BeAfter(DateTimeOffset.Now.AddHours(-1));
            result.Current.Provider.Should().BeEquivalentTo(new ProviderSummary()
            {
                Id = "provider1",
                Name = "Provider 1",
                UKPRN = "123",
            });
            result.Current.Value.Should().Be(12345.66M);
            result.Current.Version.Should().Be(1);
            result.Policy.Should().BeEquivalentTo(new Reference("subpolicy2", "SubPolicy 2"));
            result.ParentPolicy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));

            resultsList[1].CalculationSpecification.Should().BeEquivalentTo(new Reference("calc1", "Calculation 1 - Policy 2"));
            resultsList[1].ProviderId.Should().Be("provider1");
            resultsList[1].Current.Value.Should().Be(12345.2M);
            resultsList[1].Policy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
            resultsList[1].ParentPolicy.Should().BeNull();
            resultsList[1].Current.CalculationType.Should().Be(PublishedCalculationType.Funding);

            resultsList[2].CalculationSpecification.Should().BeEquivalentTo(new Reference("calc2", "Calculation 2 - Policy 2"));
            resultsList[2].ProviderId.Should().Be("provider1");
            resultsList[2].Current.Value.Should().Be(12345.3M);
            resultsList[2].Policy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
            resultsList[2].ParentPolicy.Should().BeNull();
            resultsList[2].Current.CalculationType.Should().Be(PublishedCalculationType.Funding);

            resultsList[3].CalculationSpecification.Should().BeEquivalentTo(new Reference("calc1", "Calculation 1 - Policy 2"));
            resultsList[3].ProviderId.Should().Be("provider2");
            resultsList[3].Current.Value.Should().Be(2.2M);
            resultsList[3].Policy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
            resultsList[3].ParentPolicy.Should().BeNull();
            resultsList[3].Current.CalculationType.Should().Be(PublishedCalculationType.Funding);

            resultsList[4].CalculationSpecification.Should().BeEquivalentTo(new Reference("calc2", "Calculation 2 - Policy 2"));
            resultsList[4].ProviderId.Should().Be("provider2");
            resultsList[4].Current.Value.Should().Be(2.3M);
            resultsList[4].Policy.Should().BeEquivalentTo(new Reference("p2", "Policy 2"));
            resultsList[4].ParentPolicy.Should().BeNull();
            resultsList[4].Current.CalculationType.Should().Be(PublishedCalculationType.Funding);
        }

        static PublishedProviderResultsAssemblerService CreateAssemblerService(
            ISpecificationsRepository specificationsRepository = null, ILogger logger = null)
        {
            return new PublishedProviderResultsAssemblerService(
                specificationsRepository ?? CreateSpecificationsRepository(),
                logger ?? CreateLogger());
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
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
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA","test allocation line 1"),
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

        private static SpecificationCurrentVersion GenerateSpecificationWithPoliciesAndSubpolicies()
        {
            SpecificationCurrentVersion specification = new SpecificationCurrentVersion();

            List<Policy> policies = new List<Policy>();
            policies.Add(new Policy()
            {
                Id = "p1",
                Name = "Policy 1",
                Calculations = new List<Calculation>(),
                SubPolicies = new List<Policy>(),
            });

            policies.Add(new Policy()
            {
                Id = "p2",
                Name = "Policy 2",
                Calculations = new List<Calculation>()
                {
                     new Calculation()
                     {
                         Id = "calc1",
                         Name = "Calculation 1 - Policy 2",
                         CalculationType = CalculationType.Funding,
                         AllocationLine = new Reference("al1", "Allocation Line 1"),
                     },
                     new Calculation()
                     {
                         Id = "calc2",
                         Name = "Calculation 2 - Policy 2",
                         CalculationType = CalculationType.Funding,
                         AllocationLine = new Reference("al2", "Allocation Line 2"),
                     },
                     new Calculation()
                     {
                         Id = "calc3",
                         Name = "Calculation 3 - Policy 2",
                         CalculationType = CalculationType.Number,
                         IsPublic = true,
                         AllocationLine = new Reference("al1", "Allocation Line 1"),
                     },
                },
                SubPolicies = new List<Policy>()
                {
                    new Policy()
                    {
                        Id = "subpolicy1",
                        Name = "SubPolicy 1",
                        Calculations = new List<Calculation>()
                        {
                            new Calculation()
                            {
                                Id="subpolicy1Calc1",
                                Name = "Subpolicy 1 Calculation 1",
                                CalculationType = CalculationType.Funding,
                            },
                            new Calculation()
                            {
                                Id="subpolicy1Calc2",
                                Name = "Subpolicy 1 Calculation 2",
                                CalculationType = CalculationType.Funding,
                            },
                            new Calculation()
                            {
                                Id="subpolicy1Calc3",
                                Name = "Subpolicy 1 Calculation 3",
                                CalculationType = CalculationType.Number,
                                IsPublic = false,
                            },
                            new Calculation()
                            {
                                Id="subpolicy1Calc4",
                                Name = "Subpolicy 1 Calculation 4",
                                CalculationType = CalculationType.Number,
                                IsPublic = true,
                            }
                        }
                    },
                    new Policy()
                    {
                        Id = "subpolicy2",
                        Name = "SubPolicy 2",
                        Calculations = new List<Calculation>()
                        {
                            new Calculation()
                            {
                                Id="subpolicy2Calc1",
                                Name = "Subpolicy 2 Calculation 1",
                                CalculationType = CalculationType.Funding,
                            },
                            new Calculation()
                            {
                                Id="subpolicy2Calc2",
                                Name = "Subpolicy 2 Calculation 2",
                                CalculationType = CalculationType.Funding,
                            },
                            new Calculation()
                            {
                                Id="subpolicy2Calc3",
                                Name = "Subpolicy 2 Calculation 3",
                                CalculationType = CalculationType.Number,
                                IsPublic = false,
                            },
                            new Calculation()
                            {
                                Id="subpolicy2Calc4",
                                Name = "Subpolicy 2 Calculation 4",
                                CalculationType = CalculationType.Number,
                                IsPublic = true,
                            }
                        }
                    },
                    new Policy()
                    {
                        Id = "subpolicy3",
                        Name = "SubPolicy 3",
                        Calculations = new List<Calculation>()
                        {
                            new Calculation()
                            {
                                Id="subpolicy3Calc1",
                                Name = "Subpolicy 3 Calculation 1",
                                CalculationType = CalculationType.Number,
                                IsPublic = true,
                            }
                        }
                    }
                },
            });

            specification.Policies = policies;
            return specification;
        }

        private static IEnumerable<ProviderResult> CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines()
        {
            //Arrange
            return new[] {
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
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("AAAAA","test allocation line 1"),
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Funding,
                            AllocationLine = new Reference("BBBBB","test allocation line 2"),
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
        }

        static Reference CreateAuthor()
        {
            return new Reference("authorId", "authorName");
        }

        static Period CreateFundingPeriod(Reference reference)
        {
            return new Period
            {
                Id = reference.Id,
                Name = reference.Name,
                StartDate = DateTimeOffset.Now.AddYears(-5),
                EndDate = DateTimeOffset.Now.AddYears(5)
            };
        }
    }
}
