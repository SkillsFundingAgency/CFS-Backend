using System;
using System.Threading.Tasks;
using CacheCow.Server;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Policy.Caching.Http
{
    [TestClass]
    public class TemplateMetadataContentsTimedETagProviderTests
    {
        private HttpContext _httpContext;
        private Mock<IFundingTemplateRepository> _fundingTemplates;
        private TemplateMetadataContentsTimedETagProvider _provider;
        private RouteValueDictionary _routeValueDictionary;

        [TestInitialize]
        public void SetUp()
        {
            _routeValueDictionary = new RouteValueDictionary();
            _httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    RouteValues = _routeValueDictionary
                }
            };
            
            _fundingTemplates = new Mock<IFundingTemplateRepository>();

            _provider = new TemplateMetadataContentsTimedETagProvider(_fundingTemplates.Object,
                new PolicyResiliencePolicies
                {
                    FundingTemplateRepository = Polly.Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingPeriodRouteData()
        {
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();

            GivenTheRouteData((nameof(fundingStreamId), fundingStreamId),
                (nameof(templateVersion), templateVersion));

            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingPeriodId");
        }
        
        [TestMethod]
        public void GuardsAgainstMissingFundingStreamRouteData()
        {
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();

            GivenTheRouteData((nameof(fundingPeriodId), fundingPeriodId),
                (nameof(templateVersion), templateVersion));

            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }
        
        [TestMethod]
        public void GuardsAgainstMissingTemplateVersionRouteData()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            GivenTheRouteData((nameof(fundingPeriodId), fundingPeriodId),
                (nameof(fundingStreamId), fundingStreamId));

            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("templateVersion");
        }

        [TestMethod]
        public async Task UsesLastModifiedDateOfFundingTemplateIdentifiedInRouteDataToGenerateTimedETagHeaderValue()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();

            string blobName = new FundingTemplateVersionBlobName(fundingStreamId,
                fundingPeriodId,
                templateVersion);

            DateTimeOffset lastModified = NewRandomDateTimeOffset();
                
            GivenTheRouteData((nameof(fundingStreamId), fundingStreamId),
                (nameof(fundingPeriodId), fundingPeriodId),
                (nameof(templateVersion), templateVersion));
            AndTheLastModifiedDateForTheBlobName(blobName, lastModified);

            TimedEntityTagHeaderValue headerValue = await WhenTheRequestIsQueried();

            headerValue
                ?.ETag
                ?.Tag
                .Should()
                .Be($"\"{lastModified.ToETagString()}\"");
        }

        private async Task<TimedEntityTagHeaderValue> WhenTheRequestIsQueried()
            => await _provider.QueryAsync(_httpContext);

        private void GivenTheRouteData(params (string key, string value)[] routeData)
        {
            foreach ((string key, string value) routeValue in routeData)
            {
                _routeValueDictionary.Add(routeValue.key, routeValue.value);
            }   
        }

        private void AndTheLastModifiedDateForTheBlobName(string blobName,
            DateTimeOffset lastModifiedDate)
        {
            _fundingTemplates.Setup(_ => _.GetLastModifiedDate(blobName))
                .ReturnsAsync(lastModifiedDate);
        }
        
        private string NewRandomString() => new RandomString();

        private DateTimeOffset NewRandomDateTimeOffset() => new DateTimeOffset(new RandomDateTime());
    }
}