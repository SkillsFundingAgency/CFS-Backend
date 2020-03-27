using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class ProfileHistoryServiceTests
    {
        private Mock<IFundingStreamPaymentDatesRepository> _paymentDates;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IDateTimeProvider> _dateTimeProvider;

        private ProfileHistoryService _service;

        [TestInitialize]
        public void SetUp()
        {
            _paymentDates = new Mock<IFundingStreamPaymentDatesRepository>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();

            _service = new ProfileHistoryService(_paymentDates.Object,
                _publishedFunding.Object,
                _dateTimeProvider.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync(),
                    FundingStreamPaymentDatesRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoProviderIdSupplied(string providerId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileHistoryIsQueried(NewRandomString(),
                NewRandomString(),
                providerId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(nameof(providerId));
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoFundingStreamIdSupplied(string fundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileHistoryIsQueried(fundingStreamId,
                NewRandomString(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(nameof(fundingStreamId));
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoFundingPeriodIdSupplied(string fundingPeriodId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileHistoryIsQueried(NewRandomString(),
                fundingPeriodId,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(nameof(fundingPeriodId));
        }

        [TestMethod]
        public async Task ReturnsNotFoundIfUnableToLocatePublishedProviderVersionForSuppliedParameters()
        {
            NotFoundResult actualResult = (await WhenTheProfileHistoryIsQueried(NewRandomString(),
                NewRandomString(),
                NewRandomString())) as NotFoundResult;

            actualResult
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task CorrelatesPaymentDatesForFundingStreamWithProfileTotalsToMarkAnyInThePastAsPaid()
        {
            DateTimeOffset utcNow = DateTimeOffset.Parse("2020-03-20");

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _.WithFundingLines(NewFundingLine(
                fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithTypeValue("January")
                        .WithYear(2020)
                        .WithOccurence(1)),
                        NewProfilePeriod(pp => pp.WithTypeValue("January")
                            .WithYear(2020)
                            .WithOccurence(2)),
                        NewProfilePeriod(pp => pp.WithTypeValue("February")
                            .WithYear(2020)
                            .WithOccurence(1)),
                        NewProfilePeriod(pp => pp.WithTypeValue("February")
                            .WithYear(2020)
                            .WithOccurence(2)),
                        NewProfilePeriod(pp => pp.WithTypeValue("March")
                            .WithYear(2020)
                            .WithOccurence(1)),
                        NewProfilePeriod(pp => pp.WithTypeValue("March")
                            .WithYear(2020)
                            .WithOccurence(2)),
                        NewProfilePeriod(pp => pp.WithTypeValue("April")
                            .WithYear(2020)
                            .WithOccurence(1)),
                        NewProfilePeriod(pp => pp.WithTypeValue("April")
                            .WithYear(2020)
                            .WithOccurence(2))
                    ))))));

            FundingStreamPaymentDates paymentDates = NewPaymentDates(_ => _.WithPaymentDates(
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("January")
                    .WithOccurence(1)
                    .WithDate("2020-01-07")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("January")
                    .WithOccurence(2)
                    .WithDate("2020-01-21")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("February")
                    .WithOccurence(1)
                    .WithDate("2020-02-07")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("February")
                    .WithOccurence(2)
                    .WithDate("2020-02-21")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("March")
                    .WithOccurence(1)
                    .WithDate("2020-03-07")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("March")
                    .WithOccurence(2)
                    .WithDate("2020-03-21")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("April")
                    .WithOccurence(1)
                    .WithDate("2020-04-07")),
                NewPaymentDate(pd => pd.WithOccurence(1)
                    .WithYear(2020)
                    .WithTypeValue("April")
                    .WithOccurence(2)
                    .WithDate("2020-04-21"))
            ));

            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();

            GivenTheCurrentDateTime(utcNow);
            AndTheLatestPublishedProviderVersion(fundingStreamId, fundingPeriodId, providerId, publishedProviderVersion);
            AndThePaymentDates(fundingStreamId, fundingPeriodId, paymentDates);

            OkObjectResult result = (await WhenTheProfileHistoryIsQueried(fundingStreamId, fundingPeriodId, providerId)) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            ProfileTotal[] expectedProfileTotals = new[]
            {
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("January")
                    .WithOccurrence(1)
                    .WithIsPaid(true)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("January")
                    .WithOccurrence(2)
                    .WithIsPaid(true)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("February")
                    .WithOccurrence(1)
                    .WithIsPaid(true)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("February")
                    .WithOccurrence(2)
                    .WithIsPaid(true)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("March")
                    .WithOccurrence(1)
                    .WithIsPaid(true)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("March")
                    .WithOccurrence(2)
                    .WithIsPaid(false)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("April")
                    .WithOccurrence(1)
                    .WithIsPaid(false)),
                NewProfileTotal(_ => _.WithYear(2020)
                    .WithTypeValue("April")
                    .WithOccurrence(2)
                    .WithIsPaid(false)),
            };
            
            result
                .Value
                .Should()
                .BeEquivalentTo(expectedProfileTotals, 
                    opt => opt.WithoutStrictOrdering());
        }

        private static IEnumerable<object[]> MissingIdExamples()
        {
            yield return new object[] {null};
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }

        private async Task<IActionResult> WhenTheProfileHistoryIsQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            return await _service.GetProfileHistory(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        private void GivenTheCurrentDateTime(DateTimeOffset currentDate)
        {
            _dateTimeProvider.Setup(_ => _.UtcNow)
                .Returns(currentDate);
        }

        private void AndTheLatestPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFunding.Setup(_ => _.GetLatestPublishedProviderVersion(fundingStreamId, 
                    fundingPeriodId, 
                    providerId))
                .ReturnsAsync(publishedProviderVersion);
        }

        private void AndThePaymentDates(string fundingStreamId, string fundingPeriodId, FundingStreamPaymentDates paymentDates)
        {
            _paymentDates.Setup(_ => _.GetUpdateDates(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(paymentDates);
        }

        private ProfileTotal NewProfileTotal(Action<ProfileTotalBuilder> setUp = null)
        {
            ProfileTotalBuilder profileTotalBuilder = new ProfileTotalBuilder()
                .WithValue(0);

            setUp?.Invoke(profileTotalBuilder);
            
            return profileTotalBuilder.Build();
        }
        
        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        private ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder()
                .WithAmount(0);

            setUp?.Invoke(profilePeriodBuilder);
            
            return profilePeriodBuilder.Build();
        }

        private DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }
        
        private FundingStreamPaymentDates NewPaymentDates(Action<FundingStreamPaymentDatesBuilder> setUp = null)
        {
            FundingStreamPaymentDatesBuilder paymentDatesBuilder = new FundingStreamPaymentDatesBuilder();

            setUp?.Invoke(paymentDatesBuilder);

            return paymentDatesBuilder.Build();
        }

        private FundingStreamPaymentDate NewPaymentDate(Action<FundingStreamPaymentDateBuilder> setUp = null)
        {
            FundingStreamPaymentDateBuilder paymentDateBuilder = new FundingStreamPaymentDateBuilder();

            setUp?.Invoke(paymentDateBuilder);

            return paymentDateBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}