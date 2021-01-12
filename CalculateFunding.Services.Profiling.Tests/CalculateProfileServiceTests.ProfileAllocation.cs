using System;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Services;
using FluentAssertions;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public partial class CalculateProfileServiceTests
    {
        private IProfilePatternRepository _repo;
        private ICacheProvider _cacheProvider;
        private ILogger _logger;
        private CalculateProfileService _service;

        [TestMethod]
        public void WhenAValidPsgProfileIsProvided_ThenCorrectResultsProduced()
        {
            // Arrange
            ProfileRequest profileRequest = new ProfileRequest()
            {
                FundingLineCode = "TestFLC",
                FundingPeriodId = "AY-1920",
                FundingStreamId = "PSG",
                FundingValue = 12000,
            };

            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = new FundingStreamPeriodProfilePattern()
            {
                FundingLineId = "TestFLC",
                FundingPeriodId = "AY-1920",
                FundingStreamId = "PSG",
                FundingStreamPeriodEndDate = DateTime.Parse("2020-08-31T23:59:59"),
                FundingStreamPeriodStartDate = DateTime.Parse("2019-09-01T00:00:00"),
                ProfilePattern = new ProfilePeriodPattern[]
              {
                 new ProfilePeriodPattern(PeriodType.CalendarMonth, "October", DateTime.Parse("2019-09-01T00:00:00"), DateTime.Parse("2020-03-31T23:59:59"), 1920, 1, "FY-1920", 58.33333333M),
                 new ProfilePeriodPattern(PeriodType.CalendarMonth, "April", DateTime.Parse("2020-04-01T00:00:00"), DateTime.Parse("2020-08-31T23:59:59"), 2021, 1, "FY-2021", 41.6666667M),
              },
            };

            // Act
            AllocationProfileResponse result = _service.ProfileAllocation(profileRequest, fundingStreamPeriodProfilePattern, profileRequest.FundingValue);

            // Assert
            result
                .Should()
                .BeEquivalentTo(new AllocationProfileResponse()
                {
                    DeliveryProfilePeriods = new DeliveryProfilePeriod[]
                    {
                        new DeliveryProfilePeriod()
                        {
                            DistributionPeriod = "FY-1920",
                            Occurrence = 1,
                            ProfileValue = 7000,
                            Type = PeriodType.CalendarMonth,
                            TypeValue = "October",
                            Year = 1920,
                        },
                        new DeliveryProfilePeriod()
                        {
                            DistributionPeriod = "FY-2021",
                            Occurrence = 1,
                            ProfileValue = 5000,
                            Type = PeriodType.CalendarMonth,
                            TypeValue = "April",
                            Year = 2021,
                        }

                    },
                    DistributionPeriods = new DistributionPeriods[]
                    {
                        new DistributionPeriods()
                        {
                            DistributionPeriodCode = "FY-1920",
                            Value = 7000,
                        },
                        new DistributionPeriods()
                        {
                            DistributionPeriodCode = "FY-2021",
                            Value = 5000,
                        }
                    }
                });
        }

        [TestMethod]
        public void WhenAValidProfileIsProvidedWithMultipleProfilesPerDistributionPeriod_ThenCorrectResultsProduced()
        {
            // Arrange
            ProfileRequest profileRequest = new ProfileRequest()
            {
                FundingLineCode = "TestFLC",
                FundingPeriodId = "AY-1920",
                FundingStreamId = "PSG",
                FundingValue = 12000,
            };

            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = new FundingStreamPeriodProfilePattern()
            {
                FundingLineId = "TestFLC",
                FundingPeriodId = "AY-1920",
                FundingStreamId = "PSG",
                FundingStreamPeriodEndDate = DateTime.Parse("2020-08-31T23:59:59"),
                FundingStreamPeriodStartDate = DateTime.Parse("2019-09-01T00:00:00"),
                ProfilePattern = new ProfilePeriodPattern[]
              {
                 new ProfilePeriodPattern(PeriodType.CalendarMonth, "October", DateTime.Parse("2019-09-01T00:00:00"), DateTime.Parse("2020-03-31T23:59:59"), 1920, 1, "FY-1920", 38.33333333M),
                 new ProfilePeriodPattern(PeriodType.CalendarMonth, "November", DateTime.Parse("2019-09-01T00:00:00"), DateTime.Parse("2020-03-31T23:59:59"), 1920, 1, "FY-1920", 20M),
                 new ProfilePeriodPattern(PeriodType.CalendarMonth, "April", DateTime.Parse("2020-04-01T00:00:00"), DateTime.Parse("2020-08-31T23:59:59"), 1920, 1, "FY-2021", 58.33333333M),
              }
            };

            // Act
            AllocationProfileResponse result = _service.ProfileAllocation(profileRequest, fundingStreamPeriodProfilePattern, profileRequest.FundingValue);

            // Assert
            result
                .Should()
                .BeEquivalentTo(new AllocationProfileResponse()
                {
                    DeliveryProfilePeriods = new DeliveryProfilePeriod[]
                    {
                        new DeliveryProfilePeriod()
                        {
                            DistributionPeriod = "FY-1920",
                            Occurrence = 1,
                            ProfileValue = 4600,
                            Type = PeriodType.CalendarMonth,
                            TypeValue = "October",
                            Year = 1920,
                        },
                        new DeliveryProfilePeriod()
                        {
                            DistributionPeriod = "FY-1920",
                            Occurrence = 1,
                            ProfileValue = 2400,
                            Type = PeriodType.CalendarMonth,
                            TypeValue = "November",
                            Year = 1920,
                        },
                        new DeliveryProfilePeriod()
                        {
                            DistributionPeriod = "FY-2021",
                            Occurrence = 1,
                            ProfileValue = 5000,
                            Type = PeriodType.CalendarMonth,
                            TypeValue = "April",
                            Year = 1920,
                        }

                    },
                    DistributionPeriods = new DistributionPeriods[]
                    {
                        new DistributionPeriods()
                        {
                            DistributionPeriodCode = "FY-1920",
                            Value = 7000,
                        },
                        new DistributionPeriods()
                        {
                            DistributionPeriodCode = "FY-2021",
                            Value = 5000,
                        }
                    }
                });
        }



        [TestInitialize]
        public void Setup()
        {
            _repo = NSubstitute.Substitute.For<IProfilePatternRepository>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _logger = Substitute.For<ILogger>();

            _service = new CalculateProfileService(
                _repo,
                _cacheProvider,
                new Mock<IValidator<ProfileBatchRequest>>().Object,
                _logger,
                new ProfilingResiliencePolicies
                {
                    Caching = Policy.NoOpAsync(),
                    ProfilePatternRepository = Policy.NoOpAsync()
                },
                new Mock<IProducerConsumerFactory>().Object,
                new FundingValueProfiler());
        }
    }
}
