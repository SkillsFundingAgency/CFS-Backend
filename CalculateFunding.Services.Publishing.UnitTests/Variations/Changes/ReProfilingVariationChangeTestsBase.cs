using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ReProfilingVariationChangeTestsBase : VariationChangeTestBase
    {
        protected Mock<IReProfilingRequestBuilder> ReProfileRequestBuilder;

        private Mock<IReProfilingResponseMapper> _reProfilingResponseMapper;
        private Mock<IProfilingApiClient> _profilingApiClient;
        private const string DoubleQuotes = "&quot;&quot;";

        protected abstract string Strategy { get; }
        protected abstract string ChangeName { get; }

        protected DateTimeOffset ProfileDate => DateTime.Now;

        [TestInitialize]
        public void ReProfilingVariationChangeTestsBaseSetUp()
        {
            _reProfilingResponseMapper = new Mock<IReProfilingResponseMapper>();
            ReProfileRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profilingApiClient = new Mock<IProfilingApiClient>();

            VariationsApplication.ReProfilingResponseMapper
                .Returns(_reProfilingResponseMapper.Object);
            VariationsApplication.ReProfilingRequestBuilder
                .Returns(ReProfileRequestBuilder.Object);
            VariationsApplication.ProfilingApiClient
                .Returns(_profilingApiClient.Object);
        }

        [TestMethod]
        public async Task GuardsAgainstNoAffectedFundingLineCodesInContext()
        {
            await WhenTheChangeIsApplied();

            ArgumentNullException argumentNullException = new ArgumentNullException("AffectedFundingLineCodes");

            VariationContext.ErrorMessages
                .AnyWithNullCheck()
                .Should()
                .Be(true);

            IEnumerable<string> errorFieldValues = GetFieldValues(VariationContext.ErrorMessages.First());

            errorFieldValues
                .First()
                .Should()
                .Be(VariationContext.ProviderId);
            
            errorFieldValues
                .Skip(1)
                .First()
                .Should()
                .Be($"Unable to apply '{ChangeName}' for '{Strategy}'");
            
            errorFieldValues
                .Skip(2)
                .First()
                .Should()
                .Be(argumentNullException.Message);
        }

        [TestMethod]
        public async Task ReProfilesFundingLinesInTheRefreshStateWhereTheyShowAsAffectedFundingLineCodesWithEmptyProfilingResponse()
        {
            FundingLine fundingLineOne = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineThree = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));


            ProfilePatternKey profilePatternKey = NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode));

            ReProfileRequest reProfileRequestOne = NewReProfileRequest();
            ReProfileRequest reProfileRequestThree = NewReProfileRequest();

            ReProfileResponse reProfileResponseOne = NewReProfileResponse();
            ReProfileResponse reProfileResponseThree = NewReProfileResponse();

            DistributionPeriod[] distributionPeriodsOne = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsTwo = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsThree = NewDistributionPeriods();

            fundingLineOne.DistributionPeriods = NewDistributionPeriods(_ => _.WithDistributionPeriodId(distributionPeriodsOne.Single().DistributionPeriodId));
            fundingLineTwo.DistributionPeriods = distributionPeriodsTwo;
            fundingLineThree.DistributionPeriods = NewDistributionPeriods(_ => _.WithDistributionPeriodId(distributionPeriodsThree.Single().DistributionPeriodId));

            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineOne.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineTwo.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineThree.FundingLineCode)));
            GivenTheFundingLines(fundingLineOne, fundingLineTwo, fundingLineThree);
            GivenTheProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode)
                .WithKey(profilePatternKey.Key)));
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode, fundingLineThree.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne, RefreshState.ProfilePatternKeys.Single(_ => _.FundingLineCode == fundingLineOne.FundingLineCode).Key);
            AndTheTheReProfileRequest(fundingLineThree, reProfileRequestThree);

            await WhenTheChangeIsApplied();

            VariationContext.ErrorMessages
                    .AnyWithNullCheck()
                    .Should()
                    .Be(true);

            IEnumerable<string> errorFieldValues = GetFieldValues(VariationContext.ErrorMessages.First());
            
            errorFieldValues
                .First()
                .Should()
                .Be(VariationContext.ProviderId);

            errorFieldValues
                .Skip(1)
                .First()
                .Should()
                .Be($"Unable to apply '{ChangeName}' for '{Strategy}'");

            errorFieldValues
                .Skip(2)
                .First()
                .Should()
                .Be($"Could not re profile funding line '{fundingLineOne.FundingLineCode}' with request: '{reProfileRequestOne.AsJson().Replace("\"","\"\"")}'");
        }

        [TestMethod]
        public async Task VariationErrorLoggedWhenSkipResponseReceivedButNoCurrentProviderFundingLineExists()
        {
            FundingLine fundingLineOne = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));

            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineOne.FundingLineCode)));

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode));

            ReProfileRequest reProfileRequestOne = NewReProfileRequest();

            ReProfileResponse reProfileResponseOne = NewReProfileResponse(_ => _.WithSkipReProfiling(true));

            DistributionPeriod[] distributionPeriodsOne = NewDistributionPeriods();

            fundingLineOne.DistributionPeriods = distributionPeriodsOne;

            GivenTheFundingLines(fundingLineOne);
            GivenThePublishedProviderOriginalSnapshot(RefreshState.ProviderId,
                new Publishing.Variations.PublishedProviderSnapShots(
                    NewPublishedProvider(_ =>
                        _.WithCurrent(
                            NewPublishedProviderVersion(ppv =>
                                ppv.WithProviderId(RefreshState.ProviderId)
                            )
                        )
                    )
                )
            );
            GivenTheProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode)
                .WithKey(profilePatternKey.Key)));
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne, RefreshState.ProfilePatternKeys.Single(_ => _.FundingLineCode == fundingLineOne.FundingLineCode).Key);
            AndTheReProfileResponse(reProfileRequestOne, reProfileResponseOne);
            AndTheReProfileResponseMapping(reProfileResponseOne, distributionPeriodsOne);

            await WhenTheChangeIsApplied();

            VariationContext.ErrorMessages
                    .AnyWithNullCheck()
                    .Should()
                    .Be(true);

            IEnumerable<string> errorFieldValues = GetFieldValues(VariationContext.ErrorMessages.First());

            errorFieldValues
                .First()
                .Should()
                .Be(VariationContext.ProviderId);

            errorFieldValues
                .Skip(1)
                .First()
                .Should()
                .Be($"Unable to apply '{ChangeName}' for '{Strategy}'");

            errorFieldValues
                .Skip(2)
                .First()
                .Should()
                .Be($"Could not re profile funding line '{fundingLineOne.FundingLineCode}' as no current funding line exists");
        }

        [TestMethod]
        public async Task ReProfilingSkipsWhenSkipResponseReceived()
        {
            FundingLine fundingLineOne = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineThree = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineFour = NewFundingLine(_ => _.WithValue(100));

            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineOne.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineTwo.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineThree.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineFour.FundingLineCode)));

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode));

            ReProfileRequest reProfileRequestOne = NewReProfileRequest();
            ReProfileRequest reProfileRequestThree = NewReProfileRequest();
            ReProfileRequest reProfileRequestFour = NewReProfileRequest();

            DistributionPeriod[] distributionPeriodsOne = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsTwo = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsThree = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsFour = NewDistributionPeriods();

            ReProfileResponse reProfileResponseOne = NewReProfileResponse(_ => _.WithDeliveryProfilePeriods(NewDeliveryProfilePeriod(dp => dp.WithDistributionPeriod(distributionPeriodsOne.Single().DistributionPeriodId))));
            ReProfileResponse reProfileResponseThree = NewReProfileResponse(_ => _.WithDeliveryProfilePeriods(NewDeliveryProfilePeriod(dp => dp.WithDistributionPeriod(distributionPeriodsThree.Single().DistributionPeriodId))));
            ReProfileResponse reProfileResponseFour = NewReProfileResponse(_ => _.WithDeliveryProfilePeriods(NewDeliveryProfilePeriod(dp => dp.WithDistributionPeriod(distributionPeriodsFour.Single().DistributionPeriodId))));

            fundingLineOne.DistributionPeriods = distributionPeriodsOne;
            fundingLineTwo.DistributionPeriods = distributionPeriodsTwo;
            fundingLineThree.DistributionPeriods = NewDistributionPeriods(_ => _.WithProfilePeriods(NewProfilePeriod())
            .WithDistributionPeriodId(distributionPeriodsThree.Single().DistributionPeriodId));
            fundingLineFour.DistributionPeriods = distributionPeriodsFour;

            GivenTheFundingLines(fundingLineOne, fundingLineTwo, fundingLineThree, fundingLineFour);
            GivenThePublishedProviderOriginalSnapshot(RefreshState.ProviderId,
                new Publishing.Variations.PublishedProviderSnapShots(
                    NewPublishedProvider(_ =>
                        _.WithCurrent(
                            NewPublishedProviderVersion(ppv =>
                                ppv.WithProviderId(RefreshState.ProviderId)
                                .WithFundingLines(fundingLineOne)
                            )
                        )
                    )
                )
            );
            GivenTheProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode)
                .WithKey(profilePatternKey.Key)));
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode, fundingLineThree.FundingLineCode, fundingLineFour.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne, RefreshState.ProfilePatternKeys.Single(_ => _.FundingLineCode == fundingLineOne.FundingLineCode).Key);
            AndTheTheReProfileRequest(fundingLineThree, reProfileRequestThree);
            AndTheTheReProfileRequest(fundingLineFour, reProfileRequestFour);
            AndTheReProfileResponse(reProfileRequestOne, reProfileResponseOne);
            AndTheReProfileResponse(reProfileRequestThree, reProfileResponseThree);
            AndTheReProfileResponse(reProfileRequestFour, reProfileResponseFour);
            AndTheReProfileResponseMapping(reProfileResponseOne, distributionPeriodsOne);
            AndTheReProfileResponseMapping(reProfileResponseThree, distributionPeriodsThree);
            AndTheReProfileResponseMapping(reProfileResponseFour, distributionPeriodsFour);

            await WhenTheChangeIsApplied();

            fundingLineOne.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsOne);

            fundingLineTwo.DistributionPeriods
                .Should()
                .BeSameAs(distributionPeriodsTwo);

            fundingLineThree.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsThree);

            fundingLineFour.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsFour);
        }


        [TestMethod]
        public async Task ReProfilesFundingLinesInTheRefreshStateWhereTheyShowAsAffectedFundingLineCodes()
        {
            FundingLine fundingLineOne = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineThree = NewFundingLine(_ => _.WithValue(NewRandomNumberBetween(1, int.MaxValue)));
            FundingLine fundingLineFour = NewFundingLine(_ => _.WithValue(100));

            GivenTheVariationPointersForTheSpecification(NewVariationPointer(_ => _.WithFundingLineId(fundingLineOne.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineTwo.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineThree.FundingLineCode)),
                NewVariationPointer(_ => _.WithFundingLineId(fundingLineFour.FundingLineCode)));

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode));

            ReProfileRequest reProfileRequestOne = NewReProfileRequest();
            ReProfileRequest reProfileRequestTwo = NewReProfileRequest();
            ReProfileRequest reProfileRequestThree = NewReProfileRequest();
            ReProfileRequest reProfileRequestFour = NewReProfileRequest();

            DistributionPeriod[] distributionPeriodsOne = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsTwo = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsThree = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsFour = NewDistributionPeriods();

            ReProfileResponse reProfileResponseOne = NewReProfileResponse();
            ReProfileResponse reProfileResponseThree = NewReProfileResponse();
            ReProfileResponse reProfileResponseFour = NewReProfileResponse();

            fundingLineTwo.DistributionPeriods = NewDistributionPeriods(_ => _.WithProfilePeriods(NewProfilePeriod())
            .WithDistributionPeriodId(distributionPeriodsTwo.Single().DistributionPeriodId));
            fundingLineOne.DistributionPeriods = NewDistributionPeriods(_ => _.WithProfilePeriods(NewProfilePeriod())
            .WithDistributionPeriodId(distributionPeriodsOne.Single().DistributionPeriodId));
            fundingLineThree.DistributionPeriods = NewDistributionPeriods(_ => _.WithProfilePeriods(NewProfilePeriod())
            .WithDistributionPeriodId(distributionPeriodsThree.Single().DistributionPeriodId));
            fundingLineFour.DistributionPeriods = distributionPeriodsFour;

            FundingLine fundingLineTwoCurrent = fundingLineTwo.DeepCopy();
            fundingLineTwoCurrent.DistributionPeriods = distributionPeriodsTwo;

            GivenTheFundingLines(fundingLineOne, fundingLineTwo, fundingLineThree, fundingLineFour);
            GivenThePublishedProviderOriginalSnapshot(RefreshState.ProviderId,
                new Publishing.Variations.PublishedProviderSnapShots(
                    NewPublishedProvider(_ => _.WithCurrent(
                            NewPublishedProviderVersion(ppv =>
                                ppv.WithProviderId(RefreshState.ProviderId)
                                .WithFundingLines(fundingLineTwoCurrent)
                            )
                        ))
                )
            );
            GivenTheProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode)
                .WithKey(profilePatternKey.Key)));
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode, fundingLineTwo.FundingLineCode, fundingLineThree.FundingLineCode, fundingLineFour.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne, RefreshState.ProfilePatternKeys.Single(_ => _.FundingLineCode == fundingLineOne.FundingLineCode).Key);
            AndTheTheReProfileRequest(fundingLineTwo, reProfileRequestTwo, eTag: "ETag", variationPointerIndex: 2);
            AndTheTheReProfileRequest(fundingLineThree, reProfileRequestThree);
            AndTheTheReProfileRequest(fundingLineFour, reProfileRequestFour);
            AndTheReProfileResponse(reProfileRequestOne, reProfileResponseOne);
            AndTheReProfileResponse(reProfileRequestThree, reProfileResponseThree);
            AndTheReProfileResponse(reProfileRequestFour, reProfileResponseFour);
            AndTheReProfileResponseMapping(reProfileResponseOne, distributionPeriodsOne);
            AndTheReProfileResponseMapping(reProfileResponseThree, distributionPeriodsThree);
            AndTheReProfileResponseMapping(reProfileResponseFour, distributionPeriodsFour);

            await WhenTheChangeIsApplied();

            AndNoReProfileRequested(reProfileRequestTwo);

            FundingLine[] fundingLines = new FundingLine[] { fundingLineOne, fundingLineTwo, fundingLineThree, fundingLineFour };

            RefreshState.ReProfileAudits
                .All(_ => fundingLines.Any(fl => fl.FundingLineCode == _.FundingLineCode))
                .Should()
                .Be(true);

            fundingLineOne.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsOne);

            fundingLineTwo.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsTwo);

            fundingLineThree.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsThree);

            fundingLineFour.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsFour);
        }

        private DistributionPeriod[] NewDistributionPeriods(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            ProfilePeriod profilePeriodOne = NewProfilePeriod(_ => _.WithYear(ProfileDate.Year).WithTypeValue(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(ProfileDate.Month)).WithOccurence(1));
            ProfilePeriod profilePeriodTwo = NewProfilePeriod(_ => _.WithYear(ProfileDate.Year).WithTypeValue(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(ProfileDate.Month)).WithOccurence(2));
            distributionPeriodBuilder.WithProfilePeriods(profilePeriodOne,
                profilePeriodTwo);
            distributionPeriodBuilder.WithValue(profilePeriodOne.ProfiledValue + profilePeriodTwo.ProfiledValue);

            return new[] { distributionPeriodBuilder.Build() };
        }

        private DeliveryProfilePeriod NewDeliveryProfilePeriod(Action<DeliveryProfilePeriodBuilder> setUp = null)
        {
            DeliveryProfilePeriodBuilder deliveryProfilePeriodBuilder = new DeliveryProfilePeriodBuilder();

            setUp?.Invoke(deliveryProfilePeriodBuilder);

            return deliveryProfilePeriodBuilder.Build();
        }

        private ReProfileRequest NewReProfileRequest() => new ReProfileRequest { 
            ExistingPeriods = new[] { new ExistingProfilePeriod(), new ExistingProfilePeriod() }
        };

        protected ReProfileResponse NewReProfileResponse(Action<ReProfileResponseBuilder> setUp = null)
        {
            ReProfileResponseBuilder reProfileResponseBuilder = new ReProfileResponseBuilder();

            setUp?.Invoke(reProfileResponseBuilder);

            return reProfileResponseBuilder.Build();
        }

        private void AndTheReProfileResponse(ReProfileRequest request,
            ReProfileResponse response)
            => _profilingApiClient.Setup(_ => _.ReProfile(request))
                .ReturnsAsync(new ApiResponse<ReProfileResponse>(HttpStatusCode.OK, response));

        private void AndTheReProfileResponseMapping(ReProfileResponse response,
            DistributionPeriod[] distributionPeriods)
            => _reProfilingResponseMapper.Setup(_ => _.MapReProfileResponseIntoDistributionPeriods(response))
                .Returns(distributionPeriods);


        private void AndNoReProfileRequested(ReProfileRequest request)
        => _profilingApiClient.Verify(_ => _.ReProfile(request), Times.Never());

        protected virtual void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest,
            string key = null,
            string eTag = "ETagChanged",
            int variationPointerIndex = 1)
        {
            PublishedProviderVersion publishedProvider = VariationContext.RefreshState;

            string profilePatternKey = publishedProvider.ProfilePatternKeys?.SingleOrDefault(_ => _.FundingLineCode == fundingLine.FundingLineCode)?.Key;

            VariationContext.ProfilePatterns = (VariationContext.ProfilePatterns?.Values ?? ArraySegment<FundingStreamPeriodProfilePattern>.Empty)
                .Concat(new[] { 
                    new FundingStreamPeriodProfilePattern {
                    FundingLineId = fundingLine.FundingLineCode,
                    ProfilePatternKey = profilePatternKey,
                    ETag = eTag
                } 
            }).ToDictionary(_ => string.IsNullOrWhiteSpace(_.ProfilePatternKey) ? _.FundingLineId : $"{_.FundingLineId}-{_.ProfilePatternKey}");
            
            ReProfileAudit reProfileAudit = new ReProfileAudit
            {
                FundingLineCode = fundingLine.FundingLineCode,
                ETag = "ETag"
            };

            publishedProvider.AddOrUpdateReProfileAudit(reProfileAudit);

            ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLine.FundingLineCode,
                    key,
                    VariationContext.PriorState,
                    fundingLine.Value,
                    It.Is<ReProfileAudit>(_ => _.FundingLineCode == fundingLine.FundingLineCode),
                    null,
                    It.IsAny<Func<string, string, ReProfileAudit, int, bool>>()))
                .ReturnsAsync((reProfileRequest, ((ReProfileVariationChange)Change).ReProfileForSameAmountFunc(fundingLine.FundingLineCode, profilePatternKey, reProfileAudit, reProfileRequest.VariationPointerIndex ?? 1)));
        }

        protected virtual void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => fundingLineCodes.ForEach(_ => VariationContext.AddAffectedFundingLineCode(Strategy, _));

        private int NewRandomNumberBetween(int min,
            int max) => new RandomNumberBetween(min, max);

        private IEnumerable<string> GetFieldValues(string errorRow)
        {
            return errorRow
                .Replace("\"\"", DoubleQuotes)
                .Split("\",\"")
                .Select(_ =>
                    _.Replace("\"", "")
                    .Replace(DoubleQuotes, "\"\"")
                );
        }
    }
}