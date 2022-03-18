using CalculateFunding.Api.Publishing.Controllers;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.UnitTests.Controllers
{
    [TestClass]
    public class PublishedProviderControllerTests
    {
        private PublishedProvidersController _controller;

        private IProviderFundingPublishingService _providerFundingPublishingService;
        private IPublishedProviderStatusService _publishedProviderStatusService;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private IPublishedProviderFundingService _publishedProviderFundingService;
        private IPublishedProviderFundingStructureService _publishedProviderFundingStructureService;
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
            _providerFundingPublishingService = Substitute.For<IProviderFundingPublishingService>();
            _publishedProviderStatusService = Substitute.For<IPublishedProviderStatusService>();
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _publishedProviderFundingService = Substitute.For<IPublishedProviderFundingService>();
            _publishedProviderFundingStructureService = Substitute.For<IPublishedProviderFundingStructureService>();
            _deletePublishedProvidersService = Substitute.For<IDeletePublishedProvidersService>();

            _featureToggle = Substitute.For<IFeatureToggle>();

            _controller = new PublishedProvidersController(
                _providerFundingPublishingService,
                _publishedProviderStatusService,
                _publishedProviderVersionService,
                _publishedProviderFundingService,
                _publishedProviderFundingStructureService,
                _deletePublishedProvidersService,
                new Mock<IPublishedProviderUpdateDateService>().Object,
                _featureToggle
                );


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
                .BeOfType<OkObjectResult>();
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