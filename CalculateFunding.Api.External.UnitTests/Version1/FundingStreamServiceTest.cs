using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using IPolicyFundingStreamService = CalculateFunding.Services.Policy.Interfaces.IFundingStreamService;

namespace CalculateFunding.Api.External.UnitTests.Version1
{
    [TestClass]
    public class FundingStreamServiceTest
    {
        [TestMethod]
        public async Task GetFundingStreams_WhenServiceReturnsOkResult_ShouldReturnOkResultWithFundingStreams()
        {
            Models.Obsoleted.FundingStream fundingStream = new Models.Obsoleted.FundingStream()
            {
                AllocationLines = new List<Models.Obsoleted.AllocationLine>()
                {
                    new Models.Obsoleted.AllocationLine()
                    {
                        Id = "id",
                        Name = "name",
                        ShortName = "short-name",
                        FundingRoute = CalculateFunding.Models.Obsoleted.FundingRoute.LA,
                        IsContractRequired = true
                    }
                },
                Name = "name",
                Id = "id",
                ShortName = "short-name",
                PeriodType = new Models.Obsoleted.PeriodType
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

            IPolicyFundingStreamService mockFundingService = Substitute.For<IPolicyFundingStreamService>();
            mockFundingService.GetFundingStreams().Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockFundingService, mapper);

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
        public async Task GetFundingStreams_WhenfundingStreamIsEmpty_ShouldReturnInternalServerErrorMessage()
        {
            // Arrange
            IMapper mapper = Substitute.For<IMapper>();

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new List<Models.Obsoleted.FundingStream>());

            IPolicyFundingStreamService mockFundingService = Substitute.For<IPolicyFundingStreamService>();

            mockFundingService.GetFundingStreams().Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockFundingService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams();

            // Assert
            result
                .Should().BeOfType<OkResult>();
        }
    }
}
