using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeselectSpecificationForFundingServiceTests
    {
        private ISpecificationsApiClient _specificationsApiClient;

        private DeselectSpecificationForFundingService _service;
        
        private string _fundingStreamId;
        private string _fundingPeriodId;

        [TestInitialize]
        public void SetUp()
        {
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            
            _service = new DeselectSpecificationForFundingService(_specificationsApiClient,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync()
                }, 
                Substitute.For<ILogger>());

            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
        }

        [TestMethod]
        public void ThrowsArgumentExceptionIfNoSpecificationForStreamInPeriod()
        {
            GivenTheSpecificationSummariesInTheFundingPeriod();

            Func<Task> invocation = WhenTheSpecificationIsDeselectedForFunding;

            invocation
                .Should()
                .ThrowAsync<ArgumentOutOfRangeException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public async Task DeselectsSpecificationWithSuppliedFundingStreamForFunding()
        {
            string expectedSpecificationId = NewRandomString();
            
            GivenTheSpecificationSummariesInTheFundingPeriod(NewSpecificationSummary(_ => _.WithFundingStreamIds(NewRandomString(), NewRandomString())),
                NewSpecificationSummary(_ => _.WithId(expectedSpecificationId)
                    .WithFundingStreamIds(_fundingStreamId, NewRandomString())));
            AndDeselectingTheSpecificationIdReportsSuccess(expectedSpecificationId);

            await WhenTheSpecificationIsDeselectedForFunding();

            await ThenTheSpecificationWasDeselectedForFunding(expectedSpecificationId);
        }

        [TestMethod]
        public void ThrowsInvalidOperationExceptionIfUnableToDeselectForFunding()
        {
            string expectedSpecificationId = NewRandomString();
            
            GivenTheSpecificationSummariesInTheFundingPeriod(NewSpecificationSummary(_ => _.WithFundingStreamIds(NewRandomString(), NewRandomString())),
                NewSpecificationSummary(_ => _.WithId(expectedSpecificationId)
                    .WithFundingStreamIds(_fundingStreamId, NewRandomString())));

            Func<Task> invocation = WhenTheSpecificationIsDeselectedForFunding;

            invocation
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .Result
                .Which
                .Message
                .Should()
                .Be($"Unable to deselect specification for funding for {_fundingStreamId} {_fundingPeriodId}");
        }

        private async Task WhenTheSpecificationIsDeselectedForFunding()
        {
            await _service.DeselectSpecificationForFunding(_fundingStreamId, _fundingPeriodId);
        }

        private void GivenTheSpecificationSummariesInTheFundingPeriod(params SpecificationSummary[] specificationSummaries)
        {
            _specificationsApiClient
                .GetSpecificationsSelectedForFundingByPeriod(_fundingPeriodId)
                .Returns(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));
        }

        private void AndDeselectingTheSpecificationIdReportsSuccess(string specificationId)
        {
            _specificationsApiClient
                .DeselectSpecificationForFunding(specificationId)
                .Returns(HttpStatusCode.OK);
        }

        private async Task ThenTheSpecificationWasDeselectedForFunding(string specificationId)
        {
            await _specificationsApiClient
                .Received(1)
                .DeselectSpecificationForFunding(specificationId);
        }
        
        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder summaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(summaryBuilder);
            
            return summaryBuilder.Build();
        } 
        
        private string NewRandomString() => new RandomString();
    }
}