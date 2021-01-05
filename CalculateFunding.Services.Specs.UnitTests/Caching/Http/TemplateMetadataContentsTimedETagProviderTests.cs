using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheCow.Server;
using CalculateFunding.Services.Specifications.Caching.Http;
using CalculateFunding.Services.Specifications.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Results.UnitTests.Caching.Http
{
    [TestClass]
    public class TemplateMetadataContentsTimedETagProviderTests
    {
        private HttpContext _httpContext;
        private Mock<IFundingStructureService> _fundingStructures;
        private TemplateMetadataContentsTimedETagProvider _provider;
        private Mock<IQueryCollection> _queryCollection;

        [TestInitialize]
        public void SetUp()
        {
            _queryCollection = new Mock<IQueryCollection>();
            _queryCollection.Setup(_ => _.GetEnumerator())
                .Returns(Enumerable.Empty<KeyValuePair<string, StringValues>>().GetEnumerator);
            
            _httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    Query = _queryCollection.Object
                }
            };
            
            _fundingStructures = new Mock<IFundingStructureService>();

            _provider = new TemplateMetadataContentsTimedETagProvider(_fundingStructures.Object);
        }

        [TestMethod]
        public void GuardsAgainstMissingFundingPeriodRouteData()
        {
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();

            GivenTheQueryData((nameof(fundingStreamId), fundingStreamId),
                (nameof(specificationId), specificationId));

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
            string specificationId = NewRandomString();

            GivenTheQueryData((nameof(fundingPeriodId), fundingPeriodId),
                (nameof(specificationId), specificationId));

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
        public void GuardsAgainstMissingSpecificationIdRouteData()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            GivenTheQueryData((nameof(fundingPeriodId), fundingPeriodId),
                (nameof(fundingStreamId), fundingStreamId));

            Func<Task<TimedEntityTagHeaderValue>> invocation = WhenTheRequestIsQueried;

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task UsesLastModifiedDateOfFundingTemplateIdentifiedInRouteDataToGenerateTimedETagHeaderValue()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();

            DateTimeOffset lastModified = NewRandomDateTimeOffset();
                
            GivenTheQueryData((nameof(fundingStreamId), fundingStreamId),
                (nameof(fundingPeriodId), fundingPeriodId),
                (nameof(specificationId), specificationId));
            AndTheLastModifiedDateForTheFundingStructure(fundingStreamId,
                fundingPeriodId,
                specificationId,
                lastModified);

            TimedEntityTagHeaderValue headerValue = await WhenTheRequestIsQueried();

            headerValue
                ?.ETag
                ?.Tag
                .Should()
                .Be($"\"{lastModified.ToETagString()}\"");
        }

        private async Task<TimedEntityTagHeaderValue> WhenTheRequestIsQueried()
            => await _provider.QueryAsync(_httpContext);

        private void GivenTheQueryData(params (string key, string value)[] queryData)
        {
            foreach ((string key, string value) queryValue in queryData)
            {
                StringValues expectedValue = queryValue.value;

                _queryCollection.Setup(_ => _.TryGetValue(queryValue.key, out expectedValue))
                    .Returns(true);
            }   
        }

        private void AndTheLastModifiedDateForTheFundingStructure(string fundingStreamId,
            string fundingPeriodId,
            string specificationId,
            DateTimeOffset lastModifiedDate)
        {
            _fundingStructures.Setup(_ => _.GetFundingStructureTimeStamp(fundingStreamId,
                    fundingPeriodId,
                    specificationId))
                .ReturnsAsync(lastModifiedDate);
        }
        
        private string NewRandomString() => new RandomString();

        private DateTimeOffset NewRandomDateTimeOffset() => new DateTimeOffset(new RandomDateTime());
    }
}