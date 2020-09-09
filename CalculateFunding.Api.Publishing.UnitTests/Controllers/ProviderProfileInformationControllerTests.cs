using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.Publishing.UnitTests.Controllers
{
    [TestClass]
    public class ProviderProfileInformationControllerTests
    {
        private ProviderProfileInformationController _controller;

        private IProfileTotalsService _profileTotalsService;

        private string _fundingStreamId;
        private string _fundPeriodId;
        private string _providerId;
        private string _correlationId;
        private string _userId;
        private string _userName;

        [TestInitialize]
        public void SetUp()
        {
            _profileTotalsService = Substitute.For<IProfileTotalsService>();

            _controller = new ProviderProfileInformationController(
                _profileTotalsService);

            _fundingStreamId = NewRandomString();
            _fundPeriodId = NewRandomString();
            _providerId = NewRandomString();
            _correlationId = NewRandomString();
            _userId = NewRandomString();
            _userName = NewRandomString();

            HttpContext context = Substitute.For<HttpContext>();
            HttpRequest request = Substitute.For<HttpRequest>();
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, _userId),
                new Claim(ClaimTypes.Name, _userName)
            }));

            context.Request.Returns(request);
            request.HttpContext.Returns(context);
            context.User.Returns(claimsPrincipal);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            request.Headers.Returns(new HeaderDictionary(new Dictionary<string, StringValues>
            {
                {"sfa-correlationId", new StringValues(_correlationId)}
            }));
        }

        [TestMethod]
        public async Task GetLatestProfileTotalsForPublishedProviderDelegatesToProfileTotalService()
        {
            IActionResult expectedResult = Substitute.For<IActionResult>();

            GivenTheProfilesTotalResponse(expectedResult);

            IActionResult actualResult = await _controller.GetLatestProfileTotalsForPublishedProvider(_fundingStreamId,
                _fundPeriodId,
                _providerId);

            actualResult
                .Should()
                .BeSameAs(expectedResult);
        }

        [TestMethod]
        public async Task GetAllProfileTotalsForPublishedProviderDelegatesToProfileTotalService()
        {
            IActionResult expectedResult = Substitute.For<IActionResult>();

            GivenAllTheProfilesTotalResponse(expectedResult);

            IActionResult actualResult = await _controller.GetAllReleasedProfileTotalsForPublishedProvider(_fundingStreamId,
                _fundPeriodId,
                _providerId);

            actualResult
                .Should()
                .BeSameAs(expectedResult);
        }

        private void GivenTheProfilesTotalResponse(IActionResult result)
        {
            _profileTotalsService
                .GetPaymentProfileTotalsForFundingStreamForProvider(_fundingStreamId,
                    _fundPeriodId,
                    _providerId)
                .Returns(result);
        }

        private void GivenAllTheProfilesTotalResponse(IActionResult result)
        {
            _profileTotalsService
                .GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(_fundingStreamId,
                    _fundPeriodId,
                    _providerId)
                .Returns(result);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}