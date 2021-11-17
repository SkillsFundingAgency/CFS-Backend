using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Errors;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderProfilingServiceTests
    {
        private PublishedProviderProfilingService _service;
        private Mock<IPublishedFundingRepository> _publishedFundingRepository;
        private Mock<IPublishedProviderErrorDetection> _publishedProviderErrorDetection;
        private Mock<IProfilingService> _profilingService;
        private Mock<IPublishedProviderVersioningService> _publishedProviderVersioningService;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IReProfilingRequestBuilder> _reProfilingRequestBuilder;
        private Mock<IProfilingApiClient> _profiling;
        private Mock<IPoliciesService> _policiesService;

        private Reference _author;

        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _providerId;

        private string _distributionPeriod1Id;
        private string _profilePeriod1TypeValue;
        private ProfilePeriodType _profilePeriod1Type;
        private decimal _profilePeriod1ProfiledAmount;
        private int _profilePeriod1Year;
        private int _profilePeriod1Occurence;

        private string _distributionPeriod2Id;
        private string _profilePeriod2TypeValue;
        private ProfilePeriodType _profilePeriod2Type;
        private decimal _profilePeriod2ProfiledAmount;
        private int _profilePeriod2Year;
        private int _profilePeriod2Occurence;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();
            _publishedProviderErrorDetection = new Mock<IPublishedProviderErrorDetection>();
            _profilingService = new Mock<IProfilingService>();
            _publishedProviderVersioningService = new Mock<IPublishedProviderVersioningService>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _reProfilingRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profiling = new Mock<IProfilingApiClient>();
            _policiesService = new Mock<IPoliciesService>();

            _author = NewReference();

            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _providerId = NewRandomString();

            _distributionPeriod1Id = NewRandomString();
            _profilePeriod1TypeValue = NewRandomString();
            _profilePeriod1Type = NewRandomEnum<ProfilePeriodType>();
            _profilePeriod1ProfiledAmount = NewRandomNumberBetween(1, 100);
            _profilePeriod1Year = NewRandomNumberBetween(1, 100);
            _profilePeriod1Occurence = NewRandomNumberBetween(1, 100);

            _distributionPeriod2Id = NewRandomString();
            _profilePeriod2TypeValue = NewRandomString();
            _profilePeriod2Type = NewRandomEnum<ProfilePeriodType>();
            _profilePeriod2ProfiledAmount = NewRandomNumberBetween(1, 100);
            _profilePeriod2Year = NewRandomNumberBetween(1, 100);
            _profilePeriod2Occurence = NewRandomNumberBetween(1, 100);

            _service = new PublishedProviderProfilingService(
                _publishedFundingRepository.Object,
                _publishedProviderErrorDetection.Object,
                _profilingService.Object,
                _publishedProviderVersioningService.Object,
                _specificationsApiClient.Object,
                _reProfilingRequestBuilder.Object,
                _profiling.Object,
                _policiesService.Object,
               new ReProfilingResponseMapper(),
                Logger.None,
             
                PublishingResilienceTestHelper.GenerateTestPolicies());
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfFundingStreamIdMissing()
        {
            Func<Task> invocation
                = () => WhenProfilePatternKeyIsAssigned(null, null, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == "Value cannot be null. (Parameter 'fundingStreamId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfFundingProfileIdMissing()
        {
            Func<Task> invocation
                = () => WhenProfilePatternKeyIsAssigned(_fundingStreamId, null, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == "Value cannot be null. (Parameter 'fundingPeriodId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfProviderIdMissing()
        {
            Func<Task> invocation
                = () => WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == "Value cannot be null. (Parameter 'providerId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfProfilePatternKeyMissing()
        {
            GivenTheFundingConfiguration(true);
            Func<Task> invocation
                = () => WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == "Value cannot be null. (Parameter 'profilePatternKey')");
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsBadRequestIfFundingConfigurationIsNotEnableedUserEditableRuleBasedProfiles()
        {
            string fundingLineCode = NewRandomString();
            string key = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv => ppv.WithProfilePatternKeys(
                    NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineCode).WithKey(key))))));
            GivenThePublishedProvider(publishedProvider);

            GivenTheFundingConfiguration(false);
            ProfilePatternKey profilePatternKey = NewProfilePatternKey();
            BadRequestObjectResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey) as BadRequestObjectResult;

            result
                .Should()
                .NotBeNull();

            result
                .Value
                .Should()
                .Be($"User not allowed to edit rule based profiles for funding stream - '{_fundingStreamId}' and funding period - '{_fundingPeriodId}'");
        }


        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsNotFoundIfPublishedProviderDoesNotExist()
        {
            GivenThePublishedProvider(null);
            GivenTheFundingConfiguration(true);

            ProfilePatternKey profilePatternKey = NewProfilePatternKey();
            StatusCodeResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey) as StatusCodeResult;

            result
                .Should()
                .NotBeNull();

            result
                .StatusCode
                .Should()
                .Be((int) HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsNotModifiedIfMatchingProfilePatternKeyExists()
        {
            string fundingLineCode = NewRandomString();
            string key = NewRandomString();

            GivenTheFundingConfiguration(true);
            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(key));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv => ppv.WithProfilePatternKeys(
                    NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineCode).WithKey(key))))));
            GivenThePublishedProvider(publishedProvider);

            StatusCodeResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey) as StatusCodeResult;

            result
                .Should()
                .NotBeNull();

            result
                .StatusCode
                .Should()
                .Be((int) HttpStatusCode.NotModified);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsBadRequestIfNewPublishedProviderVersionCreationFailed()
        {
            string fundingLineCode = NewRandomString();
            string existingProfilePatternKey = NewRandomString();
            string newProfilePatterFundingKey = NewRandomString();

            GivenTheFundingConfiguration(true);
            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));

            FundingLine fundingLine = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCode));
            PublishedProviderVersion existingPublishedProviderVersion =
                NewPublishedProviderVersion(ppv => ppv
                    .WithFundingStreamId(_fundingStreamId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(existingProfilePatternKey)))
                    .WithFundingLines(fundingLine)
                );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));

            GivenThePublishedProvider(publishedProvider);

            IActionResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultShouldBe(result, HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsOkAndUpdatesPatternKeyWithNewVersion()
        {
            string fundingLineCode = NewRandomString();
            string existingProfilePatternKey = NewRandomString();
            string newProfilePatterFundingKey = NewRandomString();
            string specificationId = NewRandomString();

            GivenTheFundingConfiguration(true);
            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));

            FundingLine fundingLine = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCode));
            PublishedProviderVersion existingPublishedProviderVersion =
                NewPublishedProviderVersion(ppv => ppv
                    .WithFundingStreamId(_fundingStreamId)
                    .WithSpecificationId(specificationId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(existingProfilePatternKey)))
                    .WithFundingLines(fundingLine)
                );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));
            PublishedProviderVersion newPublishedProviderVersion = existingPublishedProviderVersion;
            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest =
                NewPublishedProviderCreateVersionRequest(_ => _
                    .WithPublishedProvider(publishedProvider)
                    .WithNewVersion(newPublishedProviderVersion));

            GivenThePublishedProvider(publishedProvider);
            AndThePublishedProviderCreateVersionRequest(publishedProvider, publishedProviderCreateVersionRequest);
            AndTheNewCreatedPublishedProvider(publishedProvider, publishedProviderCreateVersionRequest);
            AndTheProfileVariationPointers(null, specificationId);
            AndTheProfileFundingLines(profilePatternKey);
            AndTheSavePublishedProviderVersionResponse(HttpStatusCode.OK, existingPublishedProviderVersion);
            AndTheUpsertPublishedProviderResponse(HttpStatusCode.OK, publishedProvider);

            IActionResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultShouldBe(result, HttpStatusCode.OK);
            AndProfilePatternKeyWasUpdated(newPublishedProviderVersion, profilePatternKey);
            AndThePublishedProviderWasProcessed(publishedProvider);
            AndTheProfilingAuditWasUpdatedForTheFundingLine(publishedProvider, profilePatternKey.FundingLineCode, _author);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyForFundingLineWithAlreadyPaidProfilePeriodsUsesReProfileResponseResultsFromApi()
        {
            string fundingLineCode = NewRandomString();
            string existingProfilePatternKey = NewRandomString();
            string newProfilePatterFundingKey = NewRandomString();
            string specificationId = NewRandomString();

            int occurence = NewRandomNumberBetween(1, 100);
            ProfilePeriodType profilePeriodType = NewRandomEnum<ProfilePeriodType>();
            string typeValue = NewRandomString();
            int year = NewRandomNumberBetween(2019, 2021);
            string distributionPeriodId = NewRandomString();
            decimal carryOverAmount = NewRandomNumberBetween(1, int.MaxValue);

            GivenTheFundingConfiguration(true);
            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));
            ProfilePeriod paidProfilePeriod = NewProfilePeriod(pp => pp
                .WithDistributionPeriodId(distributionPeriodId)
                .WithType(profilePeriodType)
                .WithTypeValue(typeValue)
                .WithYear(year)
                .WithOccurence(occurence));

            FundingLine fundingLine = NewFundingLine(_ => _
                .WithFundingLineCode(fundingLineCode)
                .WithValue(NewRandomNumberBetween(1, int.MaxValue))
                .WithDistributionPeriods(
                    NewDistributionPeriod(dp => dp
                        .WithDistributionPeriodId(distributionPeriodId)
                        .WithProfilePeriods(paidProfilePeriod))
                ));
            PublishedProviderVersion existingPublishedProviderVersion =
                NewPublishedProviderVersion(ppv => ppv
                    .WithFundingStreamId(_fundingStreamId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithSpecificationId(specificationId)
                    .WithCustomProfiles(new FundingLineProfileOverrides { FundingLineCode = profilePatternKey.FundingLineCode })
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode)
                            .WithKey(existingProfilePatternKey)))
                    .WithFundingLines(fundingLine)
                );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));
            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = NewPublishedProviderCreateVersionRequest(_ =>
                _.WithPublishedProvider(publishedProvider)
                    .WithNewVersion(existingPublishedProviderVersion));
            IEnumerable<ProfileVariationPointer> profileVariationPointers =
                NewProfileVariationPointers(_ => _
                    .WithFundingLineId(fundingLineCode)
                    .WithFundingStreamId(_fundingStreamId)
                    .WithOccurence(occurence)
                    .WithPeriodType(profilePeriodType.ToString())
                    .WithTypeValue(typeValue)
                    .WithYear(year));

            ReProfileRequest reProfileRequest = NewReProfileRequest();
            ReProfileResponse reProfileResponse = NewReProfileResponse(_ => _.WithCarryOverAmount(carryOverAmount)
                .WithDeliveryProfilePeriods(NewDeliveryProfilePeriod(dpp => dpp.WithOccurrence(1)
                    .WithValue(10)
                    .WithYear(2021)
                    .WithTypeValue("JANUARY")
                    .WithPeriodType(PeriodType.CalendarMonth)
                    .WithDistributionPeriod("dp1")),
                    NewDeliveryProfilePeriod(dpp => dpp.WithOccurrence(1)
                        .WithValue(20)
                        .WithYear(2022)
                        .WithTypeValue("JANUARY")
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithDistributionPeriod("dp2"))));
            
            GivenThePublishedProvider(publishedProvider);
            AndThePublishedProviderCreateVersionRequest(publishedProvider, publishedProviderCreateVersionRequest);
            AndTheNewCreatedPublishedProvider(publishedProvider, publishedProviderCreateVersionRequest);
            AndTheProfileVariationPointers(profileVariationPointers, specificationId);
            AndTheReProfileRequest(profilePatternKey.FundingLineCode,
                profilePatternKey.Key,
                publishedProvider.Current,
                ProfileConfigurationType.Custom,
                fundingLine.Value,
                reProfileRequest);
            AndTheReProfileResponse(reProfileRequest, reProfileResponse);
            AndTheProfileFundingLines(profilePatternKey);
            AndTheSavePublishedProviderVersionResponse(HttpStatusCode.OK, existingPublishedProviderVersion);
            AndTheUpsertPublishedProviderResponse(HttpStatusCode.OK, publishedProvider);

            IActionResult result = await WhenProfilePatternKeyIsAssigned(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultShouldBe(result, HttpStatusCode.OK);
            AndProfilePatternKeyWasUpdated(existingPublishedProviderVersion, profilePatternKey);
            AndCustomProfileRemovedForPatternKey(existingPublishedProviderVersion, profilePatternKey);

            fundingLine.DistributionPeriods
                .Count()
                .Should()
                .Be(2);

            DistributionPeriod firstDistributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(_ => _.DistributionPeriodId == "dp1");

            firstDistributionPeriod.ProfilePeriods
                .Should()
                .BeEquivalentTo(NewProfilePeriod(_ => _.WithAmount(10)
                        .WithTypeValue("JANUARY")
                        .WithDistributionPeriodId("dp1")
                        .WithType(ProfilePeriodType.CalendarMonth)
                        .WithOccurence(1)
                        .WithYear(2021)));
            
            DistributionPeriod secondDistributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(_ => _.DistributionPeriodId == "dp2");

            secondDistributionPeriod.ProfilePeriods
                .Should()
                .BeEquivalentTo(NewProfilePeriod(_ => _.WithAmount(20)
                    .WithTypeValue("JANUARY")
                    .WithDistributionPeriodId("dp2")
                    .WithType(ProfilePeriodType.CalendarMonth)
                    .WithOccurence(1)
                    .WithYear(2022)));

            existingPublishedProviderVersion.CarryOvers
                .Should()
                .BeEquivalentTo(NewCarryOver(_ => _.WithAmount(carryOverAmount)
                    .WithFundingLineCode(fundingLineCode)
                    .WithType(ProfilingCarryOverType.CustomProfile)));
            
            AndThePublishedProviderWasProcessed(publishedProvider);
            AndTheProfilingAuditWasUpdatedForTheFundingLine(publishedProvider, profilePatternKey.FundingLineCode, _author);
        }

        private ProfilingCarryOver NewCarryOver(Action<ProfilingCarryOverBuilder> setUp = null)
        {
            ProfilingCarryOverBuilder profilingCarryOverBuilder = new ProfilingCarryOverBuilder();

            setUp?.Invoke(profilingCarryOverBuilder);
            
            return profilingCarryOverBuilder.Build();
        }

        private void AndTheReProfileRequest(string fundingLineCode,
            string profilePatternKey,
            PublishedProviderVersion publishedProviderVersion,
            ProfileConfigurationType configurationType,
            decimal? fundingLineTotal, 
            ReProfileRequest profileRequest)
        {
            _reProfilingRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLineCode,
                    profilePatternKey,
                    publishedProviderVersion,
                    configurationType,
                    fundingLineTotal,
                    null))
                .ReturnsAsync(profileRequest);
        }
        
        private ReProfileRequest NewReProfileRequest() => new ReProfileRequestTestEntityBuilder().Build();

        private ReProfileResponse NewReProfileResponse(Action<ReProfileResponseBuilder> setUp = null)
        {
            ReProfileResponseBuilder reProfileResponseBuilder = new ReProfileResponseBuilder();

            setUp?.Invoke(reProfileResponseBuilder);
            
            return reProfileResponseBuilder.Build();
        }

        private DeliveryProfilePeriod NewDeliveryProfilePeriod(Action<DeliveryProfilePeriodBuilder> setUp = null)
        {
            DeliveryProfilePeriodBuilder deliveryProfilePeriodBuilder = new DeliveryProfilePeriodBuilder();

            setUp?.Invoke(deliveryProfilePeriodBuilder);
            
            return deliveryProfilePeriodBuilder.Build();
        }
        
        private void AndTheReProfileResponse(ReProfileRequest reProfileRequest,
            ReProfileResponse reProfileResponse)
            => _profiling.Setup(_ => _.ReProfile(reProfileRequest))
                .ReturnsAsync(new ApiResponse<ReProfileResponse>(HttpStatusCode.OK, reProfileResponse));

        private void GivenThePublishedProvider(PublishedProvider publishedProvider)
        {
            _publishedFundingRepository.Setup(_ =>
                    _.GetPublishedProvider(_fundingStreamId, _fundingPeriodId, _providerId))
                .ReturnsAsync(publishedProvider);
        }

        private void AndThePublishedProviderCreateVersionRequest(PublishedProvider expectedPublishedProvider,
            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest)
        {
            _publishedProviderVersioningService.Setup(_ =>
                    _.AssemblePublishedProviderCreateVersionRequests(
                        It.Is<IEnumerable<PublishedProvider>>(pp => PublishedProviderMatches(pp, expectedPublishedProvider)),
                        It.Is<Reference>(rf => rf != null && _author != null && rf.ToString() == _author.ToString()),
                        PublishedProviderStatus.Updated,
                        null,
                        null,
                        false))
                .Returns(new[]
                {
                    publishedProviderCreateVersionRequest
                });
        }

        private void AndTheNewCreatedPublishedProvider(PublishedProvider publishedProvider,
            PublishedProviderCreateVersionRequest expectedPublishedProviderCreateVersionRequest)
        {
            _publishedProviderVersioningService.Setup(_ =>
                    _.CreateVersion(expectedPublishedProviderCreateVersionRequest))
                .ReturnsAsync(publishedProvider);
        }

        private void AndTheProfileVariationPointers(IEnumerable<ProfileVariationPointer> profileVariationPointers,
            string expectedSpecificationId)
        {
            _specificationsApiClient.Setup(_ =>
                    _.GetProfileVariationPointers(expectedSpecificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, profileVariationPointers));
        }

        private void AndTheProfileFundingLines(ProfilePatternKey profilePatternKey)
        {
            _profilingService.Setup(_ =>
                _.ProfileFundingLines(
                    It.Is<IEnumerable<FundingLine>>(fl =>
                        FundingLinesMatches(fl)),
                    It.Is<string>(fs => fs == _fundingStreamId),
                   It.Is<string>(fp => fp == _fundingPeriodId),
                    It.Is<IEnumerable<ProfilePatternKey>>(ppk =>
                        ppk.Any(k => k.FundingLineCode == profilePatternKey.FundingLineCode &&
                                     k.Key == profilePatternKey.Key)),
                    null,
                    null)
            ).Returns<IEnumerable<FundingLine>, string, string, IEnumerable<ProfilePatternKey>, string, string>(
                (fundingLines, fs, fp, keys, pt, pst) =>
            {
                FundingLine fundingLine = fundingLines.FirstOrDefault();
                fundingLine.DistributionPeriods = NewDistributionPeriods(
                    dp => dp
                        .WithDistributionPeriodId(_distributionPeriod1Id)
                        .WithProfilePeriods(NewProfilePeriods(
                            pp => pp
                                .WithAmount(_profilePeriod1ProfiledAmount)
                                .WithOccurence(_profilePeriod1Occurence)
                                .WithType(_profilePeriod1Type)
                                .WithTypeValue(_profilePeriod1TypeValue)
                                .WithYear(_profilePeriod1Year)
                        ).ToArray()),
                    dp => dp
                        .WithDistributionPeriodId(_distributionPeriod2Id)
                        .WithProfilePeriods(NewProfilePeriods(
                            pp => pp
                                .WithAmount(_profilePeriod2ProfiledAmount)
                                .WithOccurence(_profilePeriod2Occurence)
                                .WithType(_profilePeriod2Type)
                                .WithTypeValue(_profilePeriod2TypeValue)
                                .WithYear(_profilePeriod2Year)
                        ).ToArray())).ToList();

                return Task.FromResult(ArraySegment<ProfilePatternKey>.Empty.AsEnumerable());
            });
        }

        private void AndTheSavePublishedProviderVersionResponse(HttpStatusCode httpStatusCode,
            PublishedProviderVersion expectedPublishedProviderVersion)
        {
            _publishedProviderVersioningService.Setup(_ =>
                    _.SaveVersion(expectedPublishedProviderVersion))
                .ReturnsAsync(httpStatusCode);
        }

        private void AndTheUpsertPublishedProviderResponse(HttpStatusCode httpStatusCode,
            PublishedProvider expectedPublishedProvider)
        {
            _publishedFundingRepository.Setup(_ =>
                    _.UpsertPublishedProvider(expectedPublishedProvider))
                .ReturnsAsync(httpStatusCode);
        }

        private async Task<IActionResult> WhenProfilePatternKeyIsAssigned(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            ProfilePatternKey profilePatternKey) => await _service.AssignProfilePatternKey(fundingStreamId, fundingPeriodId, providerId, profilePatternKey, _author);

        private void ThenResultShouldBe(IActionResult actionResult,
            HttpStatusCode httpStatusCode)
        {
            actionResult
                .Should()
                .NotBeNull();

            actionResult
                .Should()
                .BeOfType<StatusCodeResult>();

            (actionResult as StatusCodeResult)
                .StatusCode
                .Should()
                .Be((int) httpStatusCode);
        }

        private void AndProfilePatternKeyWasUpdated(PublishedProviderVersion publishedProviderVersion,
            ProfilePatternKey profilePatternKey)
        {
            Assert.AreEqual(publishedProviderVersion.ProfilePatternKeys.SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode), profilePatternKey);
        }

        private void AndCustomProfileRemovedForPatternKey(PublishedProviderVersion publishedProviderVersion,
            ProfilePatternKey profilePatternKey)
        {
            Assert.IsTrue(publishedProviderVersion.CustomProfiles == null ? true : publishedProviderVersion.CustomProfiles.Where(_ => _.FundingLineCode == profilePatternKey.FundingLineCode).Count() == 0);
        }

        private void AndThePublishedProviderWasProcessed(PublishedProvider publishedProvider)
        {
            _publishedProviderErrorDetection.Verify(_ =>
                    _.ApplyAssignProfilePatternErrorDetection(publishedProvider, It.IsAny<PublishedProvidersContext>()),
                Times.Once);
        }

        private void AndTheProfilingAuditWasUpdatedForTheFundingLine(PublishedProvider publishedProvider,
            string fundingLineCode,
            Reference author)
        {
            publishedProvider
                .Current
                .ProfilingAudits
                .Should()
                .Contain(a => a.FundingLineCode == fundingLineCode
                              && a.User != null
                              && a.User.Id == author.Id
                              && a.User.Name == author.Name
                              && a.Date.Date == DateTime.Today);
        }

        private bool PublishedProviderMatches(IEnumerable<PublishedProvider> publishedProviders,
            PublishedProvider expectedPublishedProvider)
        {
            PublishedProvider actualPublishedProvider = publishedProviders.FirstOrDefault();

            return actualPublishedProvider == expectedPublishedProvider;
        }

        private bool FundingLinesMatches(IEnumerable<FundingLine> fundingLines)
        {
            FundingLine fundingLine = fundingLines.FirstOrDefault();

            if (fundingLine == null)
            {
                return false;
            }

            ProfilePeriod firstProfilePeriod = fundingLine.DistributionPeriods?.SingleOrDefault(_ => _.DistributionPeriodId == _distributionPeriod1Id)?.ProfilePeriods.FirstOrDefault();
            ProfilePeriod lastProfilePeriod = fundingLine.DistributionPeriods?.SingleOrDefault(_ => _.DistributionPeriodId == _distributionPeriod2Id)?.ProfilePeriods.FirstOrDefault();

            if (firstProfilePeriod == null || lastProfilePeriod == null)
            {
                return false;
            }

            return firstProfilePeriod.ProfiledValue == _profilePeriod1ProfiledAmount &&
                   firstProfilePeriod.Occurrence == _profilePeriod1Occurence &&
                   firstProfilePeriod.Type == _profilePeriod1Type &&
                   firstProfilePeriod.TypeValue == _profilePeriod1TypeValue &&
                   firstProfilePeriod.Year == _profilePeriod1Year &&
                   lastProfilePeriod.ProfiledValue == _profilePeriod2ProfiledAmount &&
                   lastProfilePeriod.Occurrence == _profilePeriod2Occurence &&
                   lastProfilePeriod.Type == _profilePeriod2Type &&
                   lastProfilePeriod.TypeValue == _profilePeriod2TypeValue &&
                   lastProfilePeriod.Year == _profilePeriod2Year;
        }

        private void AndPaidProfilePeriodExists(string distributionId,
            ProfilePeriod profilePeriod,
            ProfilePatternKey profilePatternKey)
        {
            _profilingService.Verify(_ =>
                    _.ProfileFundingLines(It.Is<IEnumerable<FundingLine>>(fl => PaidProfileFundingLinesMatches(fl, distributionId, profilePeriod)),
                        _fundingStreamId,
                        _fundingPeriodId,
                        It.Is<IEnumerable<ProfilePatternKey>>(ppk =>
                            ppk.Any(k => k.FundingLineCode == profilePatternKey.FundingLineCode &&
                                         k.Key == profilePatternKey.Key)),
                        null,
                        null),
                Times.Once);
        }

        private bool PaidProfileFundingLinesMatches(IEnumerable<FundingLine> fundingLines,
            string distributionId,
            ProfilePeriod profilePeriod)
        {
            FundingLine fundingLine = fundingLines.FirstOrDefault();

            ProfilePeriod paidProfilePeriod = fundingLine
                .DistributionPeriods
                .SingleOrDefault(_ => _.DistributionPeriodId == distributionId)
                ?.ProfilePeriods
                .FirstOrDefault();

            if (paidProfilePeriod == null)
            {
                return false;
            }

            return paidProfilePeriod.ProfiledValue == profilePeriod.ProfiledValue &&
                   paidProfilePeriod.Occurrence == profilePeriod.Occurrence &&
                   paidProfilePeriod.Type == profilePeriod.Type &&
                   paidProfilePeriod.TypeValue == profilePeriod.TypeValue &&
                   paidProfilePeriod.Year == profilePeriod.Year;
        }

        private void GivenTheFundingConfiguration(bool enableUserEditableRuleBasedProfiles)
        {
            _policiesService.Setup(_ => _.GetFundingConfiguration(_fundingStreamId, _fundingPeriodId))
                .ReturnsAsync(NewFundingConfiguration(_ =>
                _.WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithEnableUserEditableRuleBasedProfiles(enableUserEditableRuleBasedProfiles)));
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private ProfilePatternKey NewProfilePatternKey(Action<ProfilePatternKeyBuilder> setUp = null)
        {
            ProfilePatternKeyBuilder patternKeyBuilder = new ProfilePatternKeyBuilder();

            setUp?.Invoke(patternKeyBuilder);

            return patternKeyBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        private static IEnumerable<ProfilePeriod> NewProfilePeriods(params Action<ProfilePeriodBuilder>[] setUps) => setUps.Select(NewProfilePeriod);

        private static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        private static IEnumerable<DistributionPeriod> NewDistributionPeriods(params Action<DistributionPeriodBuilder>[] setUps) => setUps.Select(NewDistributionPeriod);

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static PublishedProviderCreateVersionRequest NewPublishedProviderCreateVersionRequest(Action<PublishedProviderCreateVersionRequestBuilder> setUp = null)
        {
            PublishedProviderCreateVersionRequestBuilder publishedProviderCreateVersionRequestBuilder =
                new PublishedProviderCreateVersionRequestBuilder();

            setUp?.Invoke(publishedProviderCreateVersionRequestBuilder);

            return publishedProviderCreateVersionRequestBuilder.Build();
        }

        private static ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(profileVariationPointerBuilder);

            return profileVariationPointerBuilder.Build();
        }

        protected FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private static IEnumerable<ProfileVariationPointer> NewProfileVariationPointers(params Action<ProfileVariationPointerBuilder>[] setUps) => setUps.Select(NewProfileVariationPointer);

        private string NewRandomString() => new RandomString();

        private int NewRandomNumberBetween(int min,
            int max) => new RandomNumberBetween(min, max);

        private static TEnum NewRandomEnum<TEnum>() where TEnum : struct => new RandomEnum<TEnum>();
    }
}