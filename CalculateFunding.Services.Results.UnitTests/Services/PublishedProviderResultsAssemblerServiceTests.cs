using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
        public async Task AssemblePublishedProviderResults_ButNoAllocationLineResultsFound_ReturnsEmptyProviderResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = Enumerable.Empty<ProviderResult>();

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
        public async Task AssemblePublishedProviderResults_AndOneAllocationLineFoundForFundingStream_ReturnsOneProviderResult()
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
                    },
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
                       },
                   },
                },
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
                       }
                   },
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
                       },
                   },
                },
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
                       },
                   },
                },
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
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
        public async Task AssemblePublishedProviderResults_AndTwoAllocationLineFoundForFundingStreamWithThreeCalcsButOneNotPublic_EnsuresTwoResultsAssembledWithOneCalcPerAllocation()
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                               CalculationType = CalculationType.Funding
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                               CalculationType = CalculationType.Number
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                               CalculationType = CalculationType.Funding
                           }
                       },
                   },
                },
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

            allocationLine1
                .Calculations
                .Should()
                .HaveCount(1);

            PublishedAllocationLineResultVersion allocationLine2 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "BBBBB").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine2
            .Should()
            .NotBeNull();

            allocationLine2
                .Value
                .Should()
                .Be(10);

            allocationLine2
                .Calculations
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_TwoAllocationLineFoundForFundingStreamWithThreeCalcsAndNumberIsPublic_EnsuresTwoResultsAssembledWithOneFundingCalcAndOneNumberCalcPerAllocation()
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                               CalculationType = CalculationType.Funding
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                               CalculationType = CalculationType.Number,
                               IsPublic = true
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                               CalculationType = CalculationType.Funding
                           }
                       },
                   },
                },
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

            allocationLine1
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine1
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(1);

            PublishedAllocationLineResultVersion allocationLine2 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "BBBBB").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine2
            .Should()
            .NotBeNull();

            allocationLine2
                .Value
                .Should()
                .Be(10);

            allocationLine2
                .Calculations
                .Should()
                .HaveCount(2);

            allocationLine2
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine2
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(1);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_TwoAllocationLineFoundForFundingStreamWithThreeCalcsAndNumberIsPublicAndAssociatedToAllocationLine_EnsuresTwoResultsAssembledWithOneFundingEachAndOneNumberForOneAllocation()
        {
            List<CalculationResult> calculationresults = new List<CalculationResult>
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
                    CalculationType = Models.Calcs.CalculationType.Number,
                    AllocationLine = new Reference("AAAAA","test allocation line 1")
                },
                new CalculationResult
                {
                    CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                    Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                    Value = 10,
                    CalculationType = Models.Calcs.CalculationType.Funding,
                    AllocationLine = new Reference("BBBBB","test allocation line 2"),
                }
            };

            IEnumerable<ProviderResult> providerResults = CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines(calculationresults);

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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                               CalculationType = CalculationType.Funding
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                               CalculationType = CalculationType.Number,
                               AllocationLine = new Reference("AAAAA", "test allocation line 1"),
                               IsPublic = true
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                               CalculationType = CalculationType.Funding
                           }
                       },
                   },
                },
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

            allocationLine1
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine1
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(1);

            PublishedAllocationLineResultVersion allocationLine2 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "BBBBB").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine2
            .Should()
            .NotBeNull();

            allocationLine2
                .Value
                .Should()
                .Be(10);

            allocationLine2
                .Calculations
                .Should()
                .HaveCount(1);

            allocationLine2
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine2
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(0);
        }

        [TestMethod]
        public async Task AssemblePublishedProviderResults_TwoAllocationLineFoundForFundingStreamWithFourCalcsAndNumberIsPublicAndAssociatedToAllocationLineAndOneIsBaseline_EnsuresTwoResultsAssembledWithOneFundingEachAndOneBaselineForEachAndOneNumberForOneAllocation()
        {
            List<CalculationResult> calculationresults = new List<CalculationResult>
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
                    CalculationType = Models.Calcs.CalculationType.Number,
                    AllocationLine = new Reference("AAAAA","test allocation line 1")
                },
                new CalculationResult
                {
                    CalculationSpecification = new Reference { Id = "calc-spec-id-3", Name = "calc spec name 3"},
                    Calculation = new Reference { Id = "calc-id-3", Name = "calc name 3" },
                    Value = 10,
                    CalculationType = Models.Calcs.CalculationType.Funding,
                    AllocationLine = new Reference("BBBBB","test allocation line 2"),
                },
                new CalculationResult
                {
                    CalculationSpecification = new Reference { Id = "calc-spec-id-4", Name = "calc spec name 4"},
                    Calculation = new Reference { Id = "calc-id-4", Name = "calc name 4" },
                    Value = 90,
                    CalculationType = Models.Calcs.CalculationType.Baseline,
                }
            };

            IEnumerable<ProviderResult> providerResults = CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines(calculationresults);

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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                               CalculationType = CalculationType.Funding
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                               CalculationType = CalculationType.Number,
                               AllocationLine = new Reference("AAAAA", "test allocation line 1"),
                               IsPublic = true
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                               CalculationType = CalculationType.Funding
                           },
                            new Calculation()
                           {
                               Id = "calc-spec-id-4",
                               CalculationType = CalculationType.Baseline
                           }
                       },
                   },
                },
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

            allocationLine1
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine1
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(1);

            allocationLine1
              .Calculations
              .Where(m => m.CalculationType == PublishedCalculationType.Baseline)
              .Should()
              .HaveCount(1);

        PublishedAllocationLineResultVersion allocationLine2 = results.Where(f => f.FundingStreamResult.AllocationLineResult.AllocationLine.Id == "BBBBB").Select(c => c.FundingStreamResult.AllocationLineResult.Current).SingleOrDefault();

            allocationLine2
            .Should()
            .NotBeNull();

            allocationLine2
                .Value
                .Should()
                .Be(10);

            allocationLine2
                .Calculations
                .Should()
                .HaveCount(2);

            allocationLine2
                .Calculations
                .Where(m => m.CalculationType == PublishedCalculationType.Funding)
                .Should()
                .HaveCount(1);

            allocationLine2
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Number)
               .Should()
               .HaveCount(0);

            allocationLine2
               .Calculations
               .Where(m => m.CalculationType == PublishedCalculationType.Baseline)
               .Should()
               .HaveCount(1);
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
                },
                Policies = new List<Policy>()
                {
                   new Policy()
                   {
                       Id = "policy1",
                       Calculations = new List<Calculation>()
                       {
                           new Calculation()
                           {
                               Id = "calc-spec-id-1",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-2",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-3",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-4",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-5",
                           },
                           new Calculation()
                           {
                               Id = "calc-spec-id-6",
                           },
                       },
                   },
                },
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
        public async Task GeneratePublishedProviderResultsToSave_WhenNoResultsGenerated_ThenNoResultsReturned()
        {
            // Arrange
            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService();

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();

            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            // Assert
            resultsToSave
                .Should()
                .BeEmpty();

            resultsToExclude
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GeneratePublishedProviderResultsToSave_WhenNoExistingResultsExistAndResultsProvided_ThenResultsReturnedToSave()
        {
            // Arrange
            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService();

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = new AllocationLine()
                            {
                                Id = "AAAAA",
                                Name = "Allocation Line 1"
                            },
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 123,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "2",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
               .Add(new PublishedProviderResult()
               {
                   ProviderId = "2",
                   FundingStreamResult = new PublishedFundingStreamResult()
                   {
                       AllocationLineResult = new PublishedAllocationLineResult()
                       {
                           AllocationLine = new AllocationLine()
                           {
                               Id = "AAAAA",
                               Name = "Allocation Line 1"
                           },
                           Current = new PublishedAllocationLineResultVersion()
                           {
                               Status = AllocationLineStatus.Held,
                               Value = 456,
                               Major = 0,
                               Minor = 1,
                               Provider = new ProviderSummary
                               {
                                   UKPRN = "2",
                                   ProviderProfileIdType = "UKPRN"
                               }
                           }
                       }
                   }
               });

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();


            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            // Assert
            resultsToSave
                .Should()
                .HaveCount(2);

            resultsToSave
                .Should()
                .BeEquivalentTo(publishedProviderResults);

            resultsToExclude
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GeneratePublishedProviderResultsToSave_WhenExistingResultsExistAndOneResultsShouldBeUpdatedDueToValueChange_ThenResultsReturnedToSaveAndNoneExcluded()
        {
            // Arrange
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository = CreateAllocationResultsVersionRepository();

            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService(allocationResultsVersionRepository: allocationResultsVersionRepository);

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1"
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine1,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 123,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "2",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
               .Add(new PublishedProviderResult()
               {
                   ProviderId = "2",
                   FundingStreamResult = new PublishedFundingStreamResult()
                   {
                       AllocationLineResult = new PublishedAllocationLineResult()
                       {
                           AllocationLine = allocationLine1,
                           Current = new PublishedAllocationLineResultVersion()
                           {
                               Status = AllocationLineStatus.Held,
                               Value = 789,
                               Major = 0,
                               Minor = 1,
                               Provider = new ProviderSummary
                               {
                                   UKPRN = "2",
                                   ProviderProfileIdType = "UKPRN"
                               }
                           }
                       }
                   }
               });

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();
            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "1",
                Status = AllocationLineStatus.Held,
                Value = 123,
            });

            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "2",
                Status = AllocationLineStatus.Held,
                Value = 456,
            });

            allocationResultsVersionRepository
                .GetNextVersionNumber(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<int>(), incrementFromCurrentVersion: Arg.Is(true))
                .Returns(2);

            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            // Assert
            resultsToSave
                .Should()
                .HaveCount(1);

            resultsToSave
                .Should()
                .BeEquivalentTo(new List<PublishedProviderResult>() { publishedProviderResults[1] });

            resultsToExclude
                .Should()
                .BeEmpty();

            publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current
                .Version
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GeneratePublishedProviderResultsToSave_WhenExistingResultsExistAndOneResultsShouldBeUpdatedDueToValueChangeAndIsCurrenntlyApproved_ThenResultsReturnedToSaveAndNoneExcludedAndStatusIsUpdated()
        {
            // Arrange
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository = CreateAllocationResultsVersionRepository();

            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService(allocationResultsVersionRepository: allocationResultsVersionRepository);

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1"
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine1,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 123,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    Id = "1",
                                    UKPRN = "1",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
               .Add(new PublishedProviderResult()
               {
                   ProviderId = "2",
                   FundingStreamResult = new PublishedFundingStreamResult()
                   {
                       AllocationLineResult = new PublishedAllocationLineResult()
                       {
                           AllocationLine = allocationLine1,
                           Current = new PublishedAllocationLineResultVersion()
                           {
                               Status = AllocationLineStatus.Approved,
                               Value = 789,
                               Major = 0,
                               Minor = 1,
                               Provider = new ProviderSummary
                               {
                                   Id = "2",
                                   UKPRN = "2",
                                   ProviderProfileIdType = "UKPRN"
                               }
                           }
                       }
                   }
               });

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();
            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "1",
                Status = AllocationLineStatus.Held,
                Value = 123,
                Version = 1,
                Major = 0,
                Minor = 1
            });

            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "2",
                Status = AllocationLineStatus.Approved,
                Value = 456,
                Version = 1,
                Major = 0,
                Minor = 1
            });

            allocationResultsVersionRepository
                .GetNextVersionNumber(Arg.Is(publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current), Arg.Is(1), incrementFromCurrentVersion: Arg.Is(true))
                .Returns(2);

            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            string resultsJson = JsonConvert.SerializeObject(resultsToSave);

            // Assert
            resultsToSave
                .Should()
                .HaveCount(1);

            resultsToSave
                .Should()
                .BeEquivalentTo(new List<PublishedProviderResult>() { publishedProviderResults[1] });

            resultsToSave.FirstOrDefault(m => m.Summary == "UKPRN: 2, version 0.1").Should().NotBeNull();

            resultsToExclude
                .Should()
                .BeEmpty();

            publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current
                .Version
                .Should()
                .Be(0);

            publishedProviderResults.ElementAt(0).FundingStreamResult.AllocationLineResult.Current
                .Status
                .Should()
                .Be(AllocationLineStatus.Held);

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Version
                .Should()
                .Be(2);

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Status
                .Should()
                .Be(AllocationLineStatus.Updated);
        }

        [TestMethod]
        public async Task GeneratePublishedProviderResultsToSave_WhenExistingResultsExistWithMultipeAllocationLinesAndHasSavesAndAdded_ThenResultsReturnedToSaveAndNoneExcluded()
        {
            // Arrange
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository = CreateAllocationResultsVersionRepository();

            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService(allocationResultsVersionRepository: allocationResultsVersionRepository);

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1"
            };

            AllocationLine allocationLine2 = new AllocationLine()
            {
                Id = "BBBBB",
                Name = "Allocation Line 2"
            };

            AllocationLine allocationLine3 = new AllocationLine()
            {
                Id = "CCCCC",
                Name = "Allocation Line 3"
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine1,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 123,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "1",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "2",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine1,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 234,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "2",
                                    ProviderProfileIdType = "UKPRN"
                                }

                            }
                        }
                    }
                });

            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine2,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 345,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "1",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "2",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine2,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 456,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    UKPRN = "2",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });


            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();
            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "1",
                Status = AllocationLineStatus.Held,
                Value = 123,
            });

            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "2",
                Status = AllocationLineStatus.Held,
                Value = 456,
            });

            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            // Assert
            resultsToSave
                .Should()
                .HaveCount(3);

            resultsToExclude
                .Should()
                .HaveCount(0);

            resultsToSave
                .Should()
                .BeEquivalentTo(new List<PublishedProviderResult>()
                {
                    publishedProviderResults[1],
                    publishedProviderResults[2],
                    publishedProviderResults[3]
                });

            await
                allocationResultsVersionRepository
                    .Received(1)
                    .GetNextVersionNumber(Arg.Any<PublishedAllocationLineResultVersion>(), Arg.Any<int>(), null, Arg.Any<bool>());
        }

        [TestMethod]
        public async Task GeneratePublishedProviderResultsToSave_WhenExistingResultsExistAndNoResultsShouldBeUpdatedButCalcsUpdatedAndIsCurrenntlyApproved_ThenNoResultsReturnedToSaveAndNoneExcludedAndStatusIsApproved()
        {
            // Arrange
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository = CreateAllocationResultsVersionRepository();

            PublishedProviderResultsAssemblerService assembler = CreateAssemblerService(allocationResultsVersionRepository: allocationResultsVersionRepository);

            AllocationLine allocationLine1 = new AllocationLine()
            {
                Id = "AAAAA",
                Name = "Allocation Line 1"
            };

            List<PublishedProviderResult> publishedProviderResults = new List<PublishedProviderResult>();
            publishedProviderResults
                .Add(new PublishedProviderResult()
                {
                    ProviderId = "1",
                    FundingStreamResult = new PublishedFundingStreamResult()
                    {
                        AllocationLineResult = new PublishedAllocationLineResult()
                        {
                            AllocationLine = allocationLine1,
                            Current = new PublishedAllocationLineResultVersion()
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 123,
                                Major = 0,
                                Minor = 1,
                                Provider = new ProviderSummary
                                {
                                    Id = "1",
                                    UKPRN = "1",
                                    ProviderProfileIdType = "UKPRN"
                                }
                            }
                        }
                    }
                });

            publishedProviderResults
               .Add(new PublishedProviderResult()
               {
                   ProviderId = "2",
                   FundingStreamResult = new PublishedFundingStreamResult()
                   {
                       AllocationLineResult = new PublishedAllocationLineResult()
                       {
                           AllocationLine = allocationLine1,
                           Current = new PublishedAllocationLineResultVersion()
                           {
                               Status = AllocationLineStatus.Approved,
                               Value = 456,
                               Major = 0,
                               Minor = 1,
                               Provider = new ProviderSummary
                               {
                                   Id = "2",
                                   UKPRN = "2",
                                   ProviderProfileIdType = "UKPRN"
                               }
                           }
                       }
                   }
               });

            List<PublishedProviderResultExisting> existingResults = new List<PublishedProviderResultExisting>();
            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "1",
                Status = AllocationLineStatus.Held,
                Value = 123,
                Version = 1,
                Major = 0,
                Minor = 1
            });

            existingResults.Add(new PublishedProviderResultExisting()
            {
                AllocationLineId = allocationLine1.Id,
                ProviderId = "2",
                Status = AllocationLineStatus.Approved,
                Value = 456,
                Version = 1,
                Major = 0,
                Minor = 1
            });

            allocationResultsVersionRepository
                .GetNextVersionNumber(Arg.Is(publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current), Arg.Is(1), incrementFromCurrentVersion: Arg.Is(true))
                .Returns(2);

            // Act
            (IEnumerable<PublishedProviderResult> resultsToSave, IEnumerable<PublishedProviderResultExisting> resultsToExclude) = await assembler.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingResults);

            // Assert
            resultsToSave
                .Should()
                .BeEmpty();

            resultsToExclude
                .Should()
                .BeEmpty();

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Version
                .Should()
                .Be(0);

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Status
                .Should()
                .Be(AllocationLineStatus.Approved);

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Major
                .Should()
                .Be(0);

            publishedProviderResults.ElementAt(1).FundingStreamResult.AllocationLineResult.Current
                .Minor
                .Should()
                .Be(1);
        }

        static PublishedProviderResultsAssemblerService CreateAssemblerService(
            ISpecificationsRepository specificationsRepository = null,
            ILogger logger = null,
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository = null)
        {
            return new PublishedProviderResultsAssemblerService(
                specificationsRepository ?? CreateSpecificationsRepository(),
                logger ?? CreateLogger(),
                allocationResultsVersionRepository ?? CreateAllocationResultsVersionRepository());
        }

        static IVersionRepository<PublishedAllocationLineResultVersion> CreateAllocationResultsVersionRepository()
        {
            return Substitute.For<IVersionRepository<PublishedAllocationLineResultVersion>>();
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

        private static IEnumerable<ProviderResult> CreateProviderResultsWithTwoCalcsReturningTwoAllocationLines(List<CalculationResult> calculationResults = null)
        {
            //Arrange
            return new[] {
                new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = !calculationResults.IsNullOrEmpty() ? calculationResults :  new List<CalculationResult>
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
