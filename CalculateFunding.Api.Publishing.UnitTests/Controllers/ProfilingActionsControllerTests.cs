using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;

namespace CalculateFunding.Api.Publishing.UnitTests.Controllers
{
    [TestClass]
    public class ProfilingActionsControllerTests
    {
        private ProfilingActionsController _controller;
        private Mock<ICustomProfileService> _customProfileService;
        private IPublishedProviderProfilingService _publishedProviderProfilingService;
        private IFeatureToggle _featureToggle;

        private string _fundingStreamId;
        private string _fundPeriodId;
        private string _providerId;
        private string _correlationId;
        private string _userId;
        private string _userName;
        private ProfilePatternKey _profilePatternKey;

        [TestInitialize]
        public void SetUp()
        {
            _featureToggle = Substitute.For<IFeatureToggle>();
            _customProfileService = new Mock<ICustomProfileService>();
            _publishedProviderProfilingService = Substitute.For<IPublishedProviderProfilingService>();

            _controller = new ProfilingActionsController();

            _fundingStreamId = NewRandomString();
            _fundPeriodId = NewRandomString();
            _providerId = NewRandomString();
            _correlationId = NewRandomString();
            _userId = NewRandomString();
            _userName = NewRandomString();

            _profilePatternKey = new ProfilePatternKey { FundingLineCode = NewRandomString(), Key = NewRandomString() };

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
        public async Task ApplyCustomProfileDelegatesToCustomProfileService()
        {
            IActionResult expectedResult = new Mock<IActionResult>().Object;
            ApplyCustomProfileRequest request = new ApplyCustomProfileRequest();

            GivenTheApplyCustomProfileResponse(request, expectedResult);

            IActionResult actualResult = await WhenTheCustomProfileIsApplied(request);

            actualResult
                .Should()
                .BeSameAs(expectedResult);
        }

        private void GivenTheApplyCustomProfileResponse(ApplyCustomProfileRequest request, IActionResult result)
        {
            _customProfileService.Setup(_ => _.ApplyCustomProfile(request,
                    It.Is<Reference>(rf => rf.Id == _userId &&
                                           rf.Name == _userName)))
                .ReturnsAsync(result);
        }

        private async Task<IActionResult> WhenTheCustomProfileIsApplied(ApplyCustomProfileRequest request)
        {
            return await _controller.ApplyCustomProfilePattern(request, _customProfileService.Object);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyToPublishedProviderDelegatesToPublishedProviderProfilingService()
        {
            IActionResult expectedResult = Substitute.For<IActionResult>();

            GivenAssignProfilePatternKeyToPublishedProvider(expectedResult);

            IActionResult actualResult = await _controller.AssignProfilePatternKeyToPublishedProvider(_fundingStreamId,
                _fundPeriodId,
                _providerId,
                _profilePatternKey,
                _publishedProviderProfilingService);

            actualResult
                .Should()
                .BeSameAs(expectedResult);
        }

        private void GivenAssignProfilePatternKeyToPublishedProvider(IActionResult result)
        {
            _publishedProviderProfilingService
                .AssignProfilePatternKey(_fundingStreamId,
                    _fundPeriodId,
                    _providerId,
                    _profilePatternKey,
                    Arg.Any<Reference>())
                .Returns(result);
        }


        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}