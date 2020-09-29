using CalculateFunding.Services.Publishing.Caching.Http;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using CacheCow.Server;
using FluentAssertions;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Caching.Http
{
    [TestClass]
    public class PublishedProviderFundingStructureTimedEtagProviderTests
    {
        private IPublishedFundingRepository _publishedFundingRepository;
        private PublishedProviderFundingStructureTimedEtagProvider _provider;
        private DefaultHttpContext _httpContext;
        private RouteValueDictionary _routeValueDictionary;

        [TestInitialize]
        public void Setup()
        {
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _provider = new PublishedProviderFundingStructureTimedEtagProvider(_publishedFundingRepository);
            _routeValueDictionary = new RouteValueDictionary();

            _httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    RouteValues = _routeValueDictionary
                }
            };
        }

        [TestMethod]
        public void GuardAgainstMissingRouteValue()
        {
            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("publishedProviderVersionId");
        }

        [TestMethod]
        public void GivenNoPublishedProviderWhenRequestQueriedShouldThrowNonRetriableException()
        {
            string providerId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int version = NewRandomNumber();
            string publishedProviderversionId = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-{version}";
            _routeValueDictionary.Add(nameof(publishedProviderversionId), publishedProviderversionId);

            _publishedFundingRepository.GetPublishedProvider(fundingStreamId, fundingPeriodId, providerId)
                .Returns((PublishedProvider)null);

            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Published Provider not found for given FundingStreamId-{fundingStreamId}, FundingPeriodId-{fundingPeriodId}, ProviderId-{providerId}");
        }

        [TestMethod]
        public async Task GivenPublishedProviderWhenRequestQueriedShouldReturnCurrentVersionAsTimedEtagHeaderValue()
        {
            string providerId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int version = NewRandomNumber();
            string publishedProviderversionId = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-{version}";
            _routeValueDictionary.Add(nameof(publishedProviderversionId), publishedProviderversionId);

            PublishedProvider publishedProvider = new PublishedProvider() { Current = new PublishedProviderVersion() { Version = NewRandomNumber() } };
            _publishedFundingRepository.GetPublishedProvider(fundingStreamId, fundingPeriodId, providerId)
                .Returns(publishedProvider);

            TimedEntityTagHeaderValue result = await WhenTheRequestIsQueried();

            result.ETag.Tag
                .Should()
                .Be($"\"{publishedProvider.Current.Version.ToString()}\"");

        }

        private async Task<TimedEntityTagHeaderValue> WhenTheRequestIsQueried()
            => await _provider.QueryAsync(_httpContext);
        private string NewRandomString() => ((string)new RandomString()).Replace("-", string.Empty);
        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
    }
}
