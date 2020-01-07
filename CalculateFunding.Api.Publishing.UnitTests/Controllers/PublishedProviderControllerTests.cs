using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Common.Models;
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
        private IFeatureToggle _featureToggle;

        private string _fundingStreamId;
        private string _fundPeriodId;
        private string _correlationId;
        private string _userId;
        private string _userName;

        private IActionResult _result;

        [TestInitialize]
        public void SetUp()
        {
            _featureToggle = Substitute.For<IFeatureToggle>();
            _deletePublishedProvidersService = Substitute.For<IDeletePublishedProvidersService>();

            _controller = new PublishedProvidersController(Substitute.For<IProviderFundingPublishingService>(),
                Substitute.For<IPublishedProviderVersionService>(),
                _deletePublishedProvidersService,
                _featureToggle);

            _fundingStreamId = NewRandomString();
            _fundPeriodId = NewRandomString();
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

        private void GivenThatDeletingPublishedProvidersIsForbidden()
        {
            _featureToggle
                .IsDeletePublishedProviderForbidden()
                .Returns(true);
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
    }
}