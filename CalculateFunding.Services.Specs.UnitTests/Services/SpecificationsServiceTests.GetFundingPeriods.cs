using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsInCache_ReturnsFromCache()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            FundingPeriod[] fundingPeriods = {
                new FundingPeriod(),
                new FundingPeriod()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns(fundingPeriods);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                specificationsRepository
                .DidNotReceive()
                .GetFundingPeriods();
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsNotInCacheAndFailedToGetFromCosomos_ReturnsInternalServerError()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns((FundingPeriod[])null);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<InternalServerErrorResult>();

            await
                cacheProvider
                    .DidNotReceive()
                    .SetAsync<FundingPeriod[]>(Arg.Any<string>(), Arg.Any<FundingPeriod[]>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsNotInCacheButGetsFromCosmos_ReturnsFundingPeriods()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            FundingPeriod[] fundingPeriods = {
                new FundingPeriod(),
                new FundingPeriod()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns((FundingPeriod[])null);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriods()
                .Returns(fundingPeriods);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<FundingPeriod[]>(Arg.Any<string>(), Arg.Any<FundingPeriod[]>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());
        }

    }
}
