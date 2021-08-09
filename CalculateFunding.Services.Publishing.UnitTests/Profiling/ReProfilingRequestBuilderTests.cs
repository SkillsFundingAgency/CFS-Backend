using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
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
        private Mock<IProfilingApiClient> _profiling;
        private Mock<IPublishedFundingRepository> _publishedFunding;

        private ReProfilingRequestBuilder _requestBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _specifications = new Mock<ISpecificationsApiClient>();
            _profiling = new Mock<IProfilingApiClient>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            
            _requestBuilder = new ReProfilingRequestBuilder(_specifications.Object,
                _profiling.Object,
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    ProfilingApiClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        [DynamicData(nameof(MissingArgumentExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingArgumentsBeingSupplied(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string expectedParameterName)
        {
            Func<Task<ReProfileRequest>> invocation = () => WhenTheReProfileRequestIsBuilt(specificationId,
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                NewRandomString(),
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
        public void GuardsAgainstNoProfilingPatternsForTheSuppliedParameters()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();

            GivenThePublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId,
                NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp =>
                    pvp.WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)))))));

            Func<Task<ReProfileRequest>> invocation = () => WhenTheReProfileRequestIsBuilt(NewRandomString(),
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                NewRandomString(),
                ProfileConfigurationType.Custom);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Did not locate any profiling patterns for {fundingStreamId} {fundingPeriodId}");
        }

        [TestMethod]
        public void GuardsAgainstNoProfilingPatternForTheSuppliedParameters()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();

            GivenThePublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId,
                NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp =>
                    pvp.WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)))))));

            AndTheProfilingPatterns(fundingStreamId,
                fundingPeriodId,
                NewFundingStreamPeriodProfilePattern());

            Func<Task<ReProfileRequest>> invocation = () => WhenTheReProfileRequestIsBuilt(NewRandomString(),
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                NewRandomString(),
                ProfileConfigurationType.Custom);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Did not locate a profiling pattern for funding line {fundingLineCode} {fundingStreamId} {fundingPeriodId}");
        }

        [TestMethod]
        public async Task TreatsNothingAsIsPaidIfThereAreNoVariationPointersForTheFundingLine()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string specificationId = NewRandomString();
            string profilePattern = NewRandomString();
            decimal fundingLineTotal = NewRandomAmount();
            ProfileConfigurationType profileConfigurationType = NewRandomProfileConfigurationType();

            GivenThePublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId,
                NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp => 
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
                                    )))))))));
            AndTheVariationPointers(specificationId, NewProfileVariationPointer(),
                NewProfileVariationPointer(_ => _.WithFundingLineId(fundingLineCode)
                    .WithOccurence(1)
                    .WithYear(2021)
                    .WithTypeValue("January")
                    .WithPeriodType("CalenderMonth")),
                NewProfileVariationPointer(_ => _.WithFundingLineId(fundingLineCode)
                    .WithOccurence(0)
                    .WithYear(2021)
                    .WithFundingStreamId(fundingStreamId)
                    .WithTypeValue("March")
                    .WithPeriodType("CalenderMonth")));

            ReProfileRequest reProfileRequest = await WhenTheReProfileRequestIsBuilt(specificationId,
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                profilePattern,
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
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
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
        public async Task BuildsReProfileRequestsOutOfExistingFundingInformationUsingPublishedProvidersAndVariationPointers()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string specificationId = NewRandomString();
            string profilePattern = NewRandomString();
            decimal fundingLineTotal = NewRandomAmount();
            bool midYear = new RandomBoolean();
            ProfileConfigurationType profileConfigurationType = NewRandomProfileConfigurationType();

            GivenThePublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId,
                NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp => 
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
                                    )))))))));

            ReProfileRequest reProfileRequest = await WhenTheReProfileRequestIsBuilt(specificationId,
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                profilePattern,
                profileConfigurationType,
                fundingLineTotal,
                midYear);
            
            reProfileRequest
                .Should()
                .BeEquivalentTo(new ReProfileRequest
                {
                    ConfigurationType = profileConfigurationType,
                    FundingLineCode = fundingLineCode,
                    ExistingFundingLineTotal = 23 + 24 + 25 + 26,
                    FundingLineTotal = fundingLineTotal,
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    ProfilePatternKey = profilePattern,
                    MidYear = midYear,
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

        [TestMethod]
        public async Task BuildsReProfileRequestsOutOfProfilePatternsWhenNoExistingFundingInformationForPublishedProviderAndVariationPointers()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string specificationId = NewRandomString();
            string profilePattern = NewRandomString();
            decimal fundingLineTotal = NewRandomAmount();
            bool midYear = new RandomBoolean();
            ProfileConfigurationType profileConfigurationType = NewRandomProfileConfigurationType();

            GivenThePublishedProvider(fundingStreamId,
                fundingPeriodId,
                providerId,
                NewPublishedProvider(_ => _.WithCurrent(NewPublisherProviderVersion(pvp =>
                    pvp.WithFundingLines(NewFundingLine(),
                        NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCode)))))));

            AndTheProfilingPatterns(fundingStreamId,
                fundingPeriodId,
                NewFundingStreamPeriodProfilePattern(_ => _.WithPeriods(NewProfilePeriodPattern(pp => pp.WithDistributionPeriodId("dp1")
                                    .WithOccurence(0)
                                    .WithYear(2021)
                                    .WithType(PeriodType.CalendarMonth)
                                    .WithTypeValue("January")),
                                    NewProfilePeriodPattern(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithOccurence(1)
                                        .WithYear(2021)
                                        .WithType(PeriodType.CalendarMonth)
                                        .WithTypeValue("January")),
                                    NewProfilePeriodPattern(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(PeriodType.CalendarMonth)
                                        .WithTypeValue("March")),
                                    NewProfilePeriodPattern(pp => pp.WithDistributionPeriodId("dp1")
                                        .WithOccurence(0)
                                        .WithYear(2021)
                                        .WithType(PeriodType.CalendarMonth)
                                        .WithTypeValue("April"))
                                    )
                        .WithFundingLineId(fundingLineCode)
                        .WithNoPatternKey()));

            ReProfileRequest reProfileRequest = await WhenTheReProfileRequestIsBuilt(specificationId,
                fundingStreamId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                profilePattern,
                profileConfigurationType,
                fundingLineTotal,
                midYear);

            reProfileRequest
                .Should()
                .BeEquivalentTo(new ReProfileRequest
                {
                    ConfigurationType = profileConfigurationType,
                    FundingLineCode = fundingLineCode,
                    ExistingFundingLineTotal = 0,
                    FundingLineTotal = fundingLineTotal,
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    ProfilePatternKey = profilePattern,
                    MidYear = midYear,
                    VariationPointerIndex = 0,
                    ExistingPeriods = new[]
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

        private async Task<ReProfileRequest> WhenTheReProfileRequestIsBuilt(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string fundingLineCode,
            string profilePatternKey,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal = null,
            bool midYear = false)
            => await _requestBuilder.BuildReProfileRequest(fundingStreamId,
                specificationId,
                fundingPeriodId,
                providerId,
                fundingLineCode,
                profilePatternKey,
                configurationType,
                fundingLineTotal, 
                midYear);

        private void GivenThePublishedProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            PublishedProvider publishedProvider)
            => _publishedFunding.Setup(_ => _.GetPublishedProvider(fundingStreamId,
                    fundingPeriodId,
                    providerId))
                .ReturnsAsync(publishedProvider);

        private void AndTheProfilingPatterns(string fundingStreamId,
            string fundingPeriod,
            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern)
            => _profiling.Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId,
                fundingPeriod))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>(HttpStatusCode.OK,
                    new[] { fundingStreamPeriodProfilePattern }));

        private void AndTheVariationPointers(string specificationId,
            params ProfileVariationPointer[] variationPointers)
            => _specifications.Setup(_ => _.GetProfileVariationPointers(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, variationPointers));

        private static IEnumerable<object[]> MissingArgumentExamples()
        {
            yield return NewMissingArgumentsExamples(null,
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                "specificationId");
            yield return NewMissingArgumentsExamples(NewRandomString(),
               null,
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                "fundingStreamId");
            yield return NewMissingArgumentsExamples(NewRandomString(),
                NewRandomString(),
                null,
                NewRandomString(),
                NewRandomString(),
                "fundingPeriodId");
            yield return NewMissingArgumentsExamples(NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                null,
                NewRandomString(),
                "providerId");
            yield return NewMissingArgumentsExamples(NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                null,
                "fundingLineCode");
        }

        private ExistingProfilePeriod NewExististingProfilePeriod(Action<ExistingProfilePeriodBuilder> setUp = null)
        {
            ExistingProfilePeriodBuilder existingProfilePeriodBuilder = new ExistingProfilePeriodBuilder();

            setUp.Invoke(existingProfilePeriodBuilder);
            
            return existingProfilePeriodBuilder.Build();
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();
        
            setUp?.Invoke(publishedProviderBuilder);
            
            return publishedProviderBuilder.Build();
        }

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);

            return fundingStreamPeriodProfilePatternBuilder.Build();
        }

        private PublishedProviderVersion NewPublisherProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
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

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setUp = null)
        {
            ProfilePeriodPatternBuilder profilePeriodPatternBuilder = new ProfilePeriodPatternBuilder();

            setUp?.Invoke(profilePeriodPatternBuilder);

            return profilePeriodPatternBuilder.Build();
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