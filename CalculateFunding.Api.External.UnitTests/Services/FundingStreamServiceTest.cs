using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Services;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Services
{
    [TestClass]
    public class FundingStreamServiceTest
    {
        [TestMethod]
        public async Task GetFundingStreams_WhenServiceReturnsOkResult_ShouldReturnOkResultWithFundingStreams()
        {
            // Arrange
            Models.Specs.AllocationLine allocation = new Models.Specs.AllocationLine()
            {
                Id = "Id",
                Name = "Name"
            };

            List<Models.Specs.AllocationLine> allocationLineList = new List<Models.Specs.AllocationLine>();
            allocationLineList.Add(allocation);

            Models.Specs.FundingStream fundingStream = new Models.Specs.FundingStream()
            {
                AllocationLines = allocationLineList,
                Id = "Id",
                Name = "Name"
            };

            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapper = Mapper.Instance;

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new List<Models.Specs.FundingStream>
            {
                fundingStream
            });

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();
            mockSpecificationsService.GetFundingStreams(Arg.Any<HttpRequest>()).Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams(Substitute.For<HttpRequest>());

            // Assert
            result
                .Should().NotBeNull()
                .And
                .Subject.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetFundingStreams_WhenfundingStreamIsEmpty_ShouldReturnInternalServerErrorMessage()
        {
            // Arrange
            IMapper mapper = Substitute.For<IMapper>();

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new List<Models.Specs.FundingStream>());

            ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();

            mockSpecificationsService.GetFundingStreams(Arg.Any<HttpRequest>()).Returns(specServiceOkObjectResult);

            FundingStreamService fundingStreamService = new FundingStreamService(mockSpecificationsService, mapper);

            string internalErrorMessage = "Could not find any funding streams";

            InternalServerErrorResult ds = new InternalServerErrorResult(internalErrorMessage);

            // Act
            IActionResult result = await fundingStreamService.GetFundingStreams(Substitute.For<HttpRequest>());

            // Assert
            result
                .Should().BeOfType<InternalServerErrorResult>();
        }
    }
}
