using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Services;
using CalculateFunding.Api.External.V3.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using IPolicyProviderFundingVersionService = CalculateFunding.Services.Providers.Interfaces.IProviderFundingVersionService;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    [TestClass]
    public class ProviderFundingVersionServiceTests
    {
        [TestMethod]
        public async Task GetFunding_WhenFundingStreamFound_ShouldReturnOkResult()
        {
            // Arrange
            string providerFundingVersionId = "1";

            IPolicyProviderFundingVersionService mockProviderFundingVersionService = Substitute.For<IPolicyProviderFundingVersionService>();

            mockProviderFundingVersionService
                .GetProviderFundingVersions(Arg.Is(providerFundingVersionId))
                .Returns(new OkObjectResult("Valid Result"));

            ProviderFundingVersionService providerFundingVersionService = new ProviderFundingVersionService(mockProviderFundingVersionService);

            // Act
            IActionResult result = await providerFundingVersionService.GetFunding(providerFundingVersionId);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

           
        }

        [TestMethod]
        public async Task GetFunding_WhenFundingStreamNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            string providerFundingVersionId = "unknown";
           

            IPolicyProviderFundingVersionService mockProviderFundingVersionService = Substitute.For<IPolicyProviderFundingVersionService>();


            mockProviderFundingVersionService
                .GetProviderFundingVersions(Arg.Is(providerFundingVersionId))
                .Returns(new NotFoundResult());

            ProviderFundingVersionService providerFundingVersionService = new ProviderFundingVersionService(mockProviderFundingVersionService);


            // Act
            IActionResult result = await providerFundingVersionService.GetFunding(providerFundingVersionId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }
    }
}
