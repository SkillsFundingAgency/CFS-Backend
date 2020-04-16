using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.FeatureToggles;
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
    public class PublishedProviderControllerTests
    {
        private PublishedProvidersController _controller;
        private IDeletePublishedProvidersService _deletePublishedProvidersService;
        private IProfileTotalsService _profileTotalsService;
        private IPublishedProviderProfilingService _publishedProviderProfilingService;
        private IFeatureToggle _featureToggle;

        private string _fundingStreamId;
        private string _fundPeriodId;
        private string _providerId;
        private string _correlationId;
        private string _userId;
        private string _userName;
        private ProfilePatternKey _profilePatternKey;
        private Reference _author;

        private IActionResult _result;

        [TestInitialize]
        public void SetUp()
        {
            _featureToggle = Substitute.For<IFeatureToggle>();
            _deletePublishedProvidersService = Substitute.For<IDeletePublishedProvidersService>();
            _profileTotalsService = Substitute.For<IProfileTotalsService>();
            _publishedProviderProfilingService = Substitute.For<IPublishedProviderProfilingService>();

            _controller = new PublishedProvidersController(Substitute.For<IProviderFundingPublishingService>(),
                Substitute.For<IPublishedProviderVersionService>(),
                _deletePublishedProvidersService,
                _profileTotalsService,
                _publishedProviderProfilingService,
                _featureToggle);

            _fundingStreamId = NewRandomString();
            _fundPeriodId = NewRandomString();
            _providerId = NewRandomString();
            _correlationId = NewRandomString();
            _userId = NewRandomString();
            _userName = NewRandomString();
            
            _profilePatternKey = new ProfilePatternKey { FundingLineCode = NewRandomString(), Key = NewRandomString() };
            _author = NewReference(_ => _.WithId(NewRandomString()).WithName(NewRandomString()));

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

        [TestMethod]
        public async Task DeletePublishProvidersIfDeleteForbiddenReturns403Response()
        {
            GivenThatDeletingPublishedProvidersIsForbidden();

            await WhenThePublishedProvidersAreDeleted();

            _result
                .Should()
                .BeOfType<ForbidResult>();
        }

        [TestMethod]
        public async Task DeletePublishProvidersDelegatesToDeleteServiceWhenFeatureNotForbidden()
        {
            await WhenThePublishedProvidersAreDeleted();

            await _deletePublishedProvidersService
                .Received(1)
                .QueueDeletePublishedProvidersJob(_fundingStreamId,
                    _fundPeriodId,
                    _correlationId);

            _result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyToPublishedProviderDelegatesToPublishedProviderProfilingService()
        {
            IActionResult expectedResult = Substitute.For<IActionResult>();

            GivenAssignProfilePatternKeyToPublishedProvider(expectedResult);

            IActionResult actualResult = await _controller.AssignProfilePatternKeyToPublishedProvider(_fundingStreamId,
                _fundPeriodId,
                _providerId,
                _profilePatternKey);

            actualResult
                .Should()
                .BeSameAs(expectedResult);
        }

        private void GivenThatDeletingPublishedProvidersIsForbidden()
        {
            _featureToggle
                .IsDeletePublishedProviderForbidden()
                .Returns(true);
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

        private async Task WhenThePublishedProvidersAreDeleted()
        {
            _result = await _controller.DeletePublishedProviders(_fundingStreamId,
                _fundPeriodId);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }
    }
}