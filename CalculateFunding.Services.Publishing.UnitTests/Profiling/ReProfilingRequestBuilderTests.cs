using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using PeriodType = CalculateFunding.Common.ApiClient.Profiling.Models.PeriodType;
using ProfilePeriodType = CalculateFunding.Models.Publishing.ProfilePeriodType;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class ReProfilingRequestBuilderTests
    {
        private Mock<ISpecificationsApiClient> _specifications;
        private ReProfilingRequestBuilder _requestBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _specifications = new Mock<ISpecificationsApiClient>();
            
            _requestBuilder = new ReProfilingRequestBuilder(_specifications.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                });
        }

        [TestMethod]
        [DynamicData(nameof(MissingArgumentExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingArgumentsBeingSupplied(PublishedProviderVersion publishedProviderVersion,
            string fundingLineCode,
            string expectedParameterName)
        {
            Func<Task<ReProfileRequest>> invocation = () => WhenTheReProfileRequestIsBuilt(fundingLineCode,
                NewRandomString(),
                publishedProviderVersion,
                ProfileConfigurationType.Custom);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(expectedParameterName);
        }

        [TestMethod]
        public async Task TreatsNothingAsIsPaidIfThereAreNoVariationPointersForTheFundingLine()
        {
            string providerId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string profilePattern = NewRandomString();
            decimal fundingLineTotal = NewRandomAmount();
            ProfileConfigurationType profileConfigurationType = NewRandomProfileConfigurationType();

            PublishedProviderVersion publishedProviderVersion = NewPublisherProviderVersion(pvp => 
                    pvp.WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                    .WithAmount(23)
                                    .WithOccurence(0)
                                    .WithYear(2021)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(24)
                                        .WithOccurence(1)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(25)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("March")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(26)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("April"))
                                    ))))));
            AndTheVariationPointers(publishedProviderVersion.SpecificationId, NewProfileVariationPointer(),
                NewProfileVariationPointer(_ => _.WithFundingLineId(fundingLineCode)
                    .WithOccurence(1)
                    .WithYear(2021)
                    .WithTypeValue("January")
                    .WithPeriodType("CalenderMonth")),
                NewProfileVariationPointer(_ => _.WithFundingLineId(fundingLineCode)
                    .WithOccurence(0)
                    .WithYear(2021)
                    .WithFundingStreamId(publishedProviderVersion.FundingStreamId)
                    .WithTypeValue("March")
                    .WithPeriodType("CalenderMonth")));

            ReProfileRequest reProfileRequest = await WhenTheReProfileRequestIsBuilt(fundingLineCode,
                profilePattern,
                publishedProviderVersion,
                profileConfigurationType,
                fundingLineTotal);
            
            reProfileRequest
                .Should()
                .BeEquivalentTo(new ReProfileRequest
                {
                    ConfigurationType = profileConfigurationType,
                    FundingLineCode = fundingLineCode,
                    ExistingFundingLineTotal = 23 + 24 + 25 + 26,
                    FundingLineTotal = fundingLineTotal,
                    FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                    FundingStreamId = publishedProviderVersion.FundingStreamId,
                    ProfilePatternKey = profilePattern,
                    VariationPointerIndex = 2,
                    ExistingPeriods = new []
                    {
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(23)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithYear(2021)),
                        NewExististingProfilePeriod(_ => _.WithOccurrence(1)
                            .WithDistributionPeriod("dp1")
                            .WithValue(24)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithYear(2021)),
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("March")
                            .WithYear(2021)),//is paid up until here according to the variation pointer for this funding line
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("April")
                            .WithYear(2021))
                    }
                });
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(true, false)]
        [DataRow(false, false)]
        public async Task BuildsReProfileRequestsOutOfExistingFundingInformationUsingPublishedProvidersAndVariationPointers(bool midYear,
            bool opener)
        {
            string providerId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string profilePattern = NewRandomString();
            decimal fundingLineTotal = NewRandomAmount();
            ProfileConfigurationType profileConfigurationType = NewRandomProfileConfigurationType();

            PublishedProviderVersion publishedProviderVersion = NewPublisherProviderVersion(pvp => 
                    pvp.WithProvider(NewProvider(_ => _.WithStatus(opener ? Variation.Opened : Variation.Closed)))
                        .WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                    .WithAmount(23)
                                    .WithOccurence(0)
                                    .WithYear(2021)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(24)
                                        .WithOccurence(1)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(25)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("March")),
                                    NewProfilePeriod(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithAmount(26)
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("April"))
                                    ))))));

            ReProfileRequest reProfileRequest = await WhenTheReProfileRequestIsBuilt(fundingLineCode,
                profilePattern,
                publishedProviderVersion,
                profileConfigurationType,
                fundingLineTotal,
                midYear);
            
            reProfileRequest
                .Should()
                .BeEquivalentTo(new ReProfileRequest
                {
                    ConfigurationType = profileConfigurationType,
                    FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                    FundingStreamId = publishedProviderVersion.FundingStreamId,
                    FundingLineCode = fundingLineCode,
                    ExistingFundingLineTotal = 23 + 24 + 25 + 26,
                    FundingLineTotal = fundingLineTotal,
                    ProfilePatternKey = profilePattern,
                    MidYearType = midYear ? (opener ? (MidYearType?)MidYearType.Opener : MidYearType.Closure) : null,
                    VariationPointerIndex = 0,
                    ExistingPeriods = new []
                    {
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithYear(2021)),
                        NewExististingProfilePeriod(_ => _.WithOccurrence(1)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("January")
                            .WithYear(2021)),
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("March")
                            .WithYear(2021)),
                        NewExististingProfilePeriod(_ => _.WithOccurrence(0)
                            .WithDistributionPeriod("dp1")
                            .WithValue(null)
                            .WithPeriodType(PeriodType.CalendarMonth)
                            .WithTypeValue("April")
                            .WithYear(2021))
                    }
                });    
        }

        
        private async Task<ReProfileRequest> WhenTheReProfileRequestIsBuilt(string fundingLineCode,
            string profilePatternKey,
            PublishedProviderVersion publishedProviderVersion,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            bool midYear = false)
            => await _requestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                publishedProviderVersion,
                configurationType,
                fundingLineTotal, 
                midYear);

        private void AndTheVariationPointers(string specificationId,
            params ProfileVariationPointer[] variationPointers)
            => _specifications.Setup(_ => _.GetProfileVariationPointers(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, variationPointers));

        private static IEnumerable<object[]> MissingArgumentExamples()
        {
            yield return NewMissingArgumentsExamples(null,
                NewRandomString(),
                "publishedProviderVersion");
            yield return NewMissingArgumentsExamples(NewPublisherProviderVersion(),
                null,
                "fundingLineCode");
        }

        private ExistingProfilePeriod NewExististingProfilePeriod(Action<ExistingProfilePeriodBuilder> setUp = null)
        {
            ExistingProfilePeriodBuilder existingProfilePeriodBuilder = new ExistingProfilePeriodBuilder();

            setUp.Invoke(existingProfilePeriodBuilder);
            
            return existingProfilePeriodBuilder.Build();
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static PublishedProviderVersion NewPublisherProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);
            
            return publishedProviderVersionBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        private ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);
            
            return profilePeriodBuilder.Build();
        }

        private DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }

        private ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(profileVariationPointerBuilder);
            
            return profileVariationPointerBuilder.Build();
        } 

        private static object[] NewMissingArgumentsExamples(params object[] parameters) => parameters;
        
        private static string NewRandomString() => new RandomString();
        
        private static decimal NewRandomAmount() => new RandomNumberBetween(999, int.MaxValue);

        private static DateTimeOffset NewRandomDate() => new RandomDateTime();

        private static ProfileConfigurationType NewRandomProfileConfigurationType() => new RandomEnum<ProfileConfigurationType>();
    }
}