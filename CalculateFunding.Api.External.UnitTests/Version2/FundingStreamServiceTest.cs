using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Services;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version2
{
    [TestClass]
    public class FundingStreamServiceTest
    {
        [TestMethod]
        public async Task GetFundingStreams_WhenServiceReturnsOkResult_ShouldReturnOkResultWithFundingStreams()
        {
            Models.Specs.FundingStream fundingStream = new Models.Specs.FundingStream()
            {
                AllocationLines = new List<Models.Specs.AllocationLine>()
                {
                    new Models.Specs.AllocationLine()
                    {
                        Id = "id",
                        Name = "name",
                        ShortName = "short-name",
                        FundingRoute = Models.Specs.FundingRoute.LA,
                        IsContractRequired = true
                    }
                },
                Name = "name",
                Id = "id",
                ShortName = "short-name",
                RequireFinancialEnvelopes = true,
                PeriodType = new Models.Specs.PeriodType
                {
                    Id = "p1",
                    Name = "period 1",
                    StartDay = 1,
                    EndDay = 31,
                    StartMonth = 8,
                    EndMonth = 7
                }
            };

            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapper = Mapper.Instance;

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new[]
            {
                fundingStream
            });

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();
            mockSpecificationsService.GetFundingStreams().Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams();

            // Assert
            result
                .Should().NotBeNull()
                .And
                .Subject.Should().BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<FundingStream> fundingStreamResults = okObjectResult.Value as IEnumerable<FundingStream>;

            fundingStreamResults.Count().Should().Be(1);

            fundingStreamResults.First().Name.Should().Be("name");
            fundingStreamResults.First().Id.Should().Be("id");
            fundingStreamResults.First().ShortName.Should().Be("short-name");
            fundingStreamResults.First().RequireFinancialEnvelopes.Should().BeTrue();
            fundingStreamResults.First().PeriodType.Id.Should().Be("p1");
            fundingStreamResults.First().PeriodType.Name.Should().Be("period 1");
            fundingStreamResults.First().PeriodType.StartDay.Should().Be(1);
            fundingStreamResults.First().PeriodType.StartMonth.Should().Be(8);
            fundingStreamResults.First().PeriodType.EndDay.Should().Be(31);
            fundingStreamResults.First().PeriodType.EndMonth.Should().Be(7);
            fundingStreamResults.First().AllocationLines.First().Id.Should().Be("id");
            fundingStreamResults.First().AllocationLines.First().Name.Should().Be("name");
            fundingStreamResults.First().AllocationLines.First().ShortName.Should().Be("short-name");
            fundingStreamResults.First().AllocationLines.First().FundingRoute.Should().Be("LA");
            fundingStreamResults.First().AllocationLines.First().ContractRequired.Should().Be("Y");
        }

        [TestMethod]
        public async Task GetFundingStreams_WhenServiceReturnsOkResult_ShouldReturnOkResultWithMultipleFundingStreamsWithMultipleEnvelopeFlags()
        {
            Models.Specs.FundingStream fundingStreamTrue = new Models.Specs.FundingStream()
            {
                AllocationLines = new List<Models.Specs.AllocationLine>()
                {
                    new Models.Specs.AllocationLine()
                    {
                        Id = "id",
                        Name = "name",
                        ShortName = "short-name",
                        FundingRoute = Models.Specs.FundingRoute.LA,
                        IsContractRequired = true
                    }
                },
                Name = "name",
                Id = "id",
                ShortName = "short-name",
                RequireFinancialEnvelopes = true,
                PeriodType = new Models.Specs.PeriodType
                {
                    Id = "p1",
                    Name = "period 1",
                    StartDay = 1,
                    EndDay = 31,
                    StartMonth = 8,
                    EndMonth = 7
                }
            };
            Models.Specs.FundingStream fundingStreamFalse = new Models.Specs.FundingStream()
            {
                AllocationLines = new List<Models.Specs.AllocationLine>()
                {
                    new Models.Specs.AllocationLine()
                    {
                        Id = "id2",
                        Name = "name2",
                        ShortName = "short-name2",
                        FundingRoute = Models.Specs.FundingRoute.LA,
                        IsContractRequired = true
                    }
                },
                Name = "name2",
                Id = "id2",
                ShortName = "short-name2",
                RequireFinancialEnvelopes = false,
                PeriodType = new Models.Specs.PeriodType
                {
                    Id = "p2",
                    Name = "period 2",
                    StartDay = 1,
                    EndDay = 31,
                    StartMonth = 8,
                    EndMonth = 7
                }
            };

            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapper = Mapper.Instance;

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new[]
            {
                fundingStreamTrue,
                fundingStreamFalse
            });

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();
            mockSpecificationsService.GetFundingStreams().Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams();

            // Assert
            result
                .Should().NotBeNull()
                .And
                .Subject.Should().BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result.Should().BeOfType<OkObjectResult>().Subject;

            IEnumerable<FundingStream> fundingStreamResults = okObjectResult.Value as IEnumerable<FundingStream>;

            fundingStreamResults.Count().Should().Be(2);

            fundingStreamResults.First().Name.Should().Be("name");
            fundingStreamResults.First().Id.Should().Be("id");
            fundingStreamResults.First().ShortName.Should().Be("short-name");
            fundingStreamResults.First().RequireFinancialEnvelopes.Should().BeTrue();
            fundingStreamResults.Last().Name.Should().Be("name2");
            fundingStreamResults.Last().Id.Should().Be("id2");
            fundingStreamResults.Last().ShortName.Should().Be("short-name2");
            fundingStreamResults.Last().RequireFinancialEnvelopes.Should().BeFalse();
        }

        [TestMethod]
        public async Task GetFundingStreams_WhenFundingStreamIsEmpty_ShouldReturnEmptyFundingStreams()
        {
            // Arrange
            IMapper mapper = Substitute.For<IMapper>();

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new List<Models.Specs.FundingStream>());

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();

            mockSpecificationsService.GetFundingStreams().Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams();

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task GetFundingStream_WhenFundingStreamNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            string fundingStreamId = "unknown";

            IMapper mapper = Substitute.For<IMapper>();

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();

            mockSpecificationsService
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(new NotFoundResult());

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStream(fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingStream_WhenFundingStreamFound_ShouldReturnOkResult()
        {
            // Arrange
            string fundingStreamId = "PSG";

            Models.Specs.FundingStream fundingStream = new Models.Specs.FundingStream
            {
                Id = fundingStreamId,
                Name = "PE and Sport Grant",
                RequireFinancialEnvelopes = true
            };

            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapper = Mapper.Instance;

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();

            mockSpecificationsService
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(new OkObjectResult(fundingStream));

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStream(fundingStreamId);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            FundingStream actualFundingStream = okResult.Value
                .Should()
                .BeOfType<FundingStream>()
                .Subject;

            actualFundingStream.Id.Should().Be(fundingStreamId);
            actualFundingStream.Name.Should().Be(fundingStream.Name);
            actualFundingStream.RequireFinancialEnvelopes.Should().Be(fundingStream.RequireFinancialEnvelopes);
        }
    }
}
