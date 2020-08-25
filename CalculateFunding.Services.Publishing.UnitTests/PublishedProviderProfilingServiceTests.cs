using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests.Errors;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderProfilingServiceTests
    {
        private PublishedProviderProfilingService _service;
        private IPublishedFundingRepository _publishedFundingRepository;
        private ILogger _logger;
        private IPublishedProviderErrorDetection _publishedProviderErrorDetection;
        private IProfilingService _profilingService;
        private IPublishedProviderVersioningService _publishedProviderVersioningService;
        private ISpecificationsApiClient _specificationsApiClient;

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
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _publishedProviderErrorDetection = Substitute.For<IPublishedProviderErrorDetection>();
            _profilingService = Substitute.For<IProfilingService>();
            _publishedProviderVersioningService = Substitute.For<IPublishedProviderVersioningService>();
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _logger = Substitute.For<ILogger>();

            _author = NewReference(_ => _.WithId(NewRandomString()).WithName(NewRandomString()));

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
                _publishedFundingRepository,
                _publishedProviderErrorDetection,
                _profilingService,
                _publishedProviderVersioningService,
                _specificationsApiClient,
                _logger,
                PublishingResilienceTestHelper.GenerateTestPolicies());
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfFundingStreamIdMissing()
        {
            Func<Task> invocation
                = () => WhenAssigningProfilePatternKey(null, null, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'fundingStreamId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfFundingProfileIdMissing()
        {
            Func<Task> invocation
                = () => WhenAssigningProfilePatternKey(_fundingStreamId, null, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'fundingPeriodId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfProviderIdMissing()
        {
            Func<Task> invocation
                = () => WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'providerId')");
        }

        [TestMethod]
        public void AssignProfilePatternKeyThrowsArgumentNullExceptionIfProfilePatternKeyMissing()
        {
            Func<Task> invocation
                = () => WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'profilePatternKey')");
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsNotFoundIfPublishedProviderDoesNotExist()
        {
            GivenGetPublishedProvider(null);

            ProfilePatternKey profilePatternKey = NewProfilePatternKey();
            StatusCodeResult result = await WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey) as StatusCodeResult;

            result
                .Should()
                .NotBeNull();

            result
                .StatusCode
                .Should()
                .Be((int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsNotModifiedIfMatchingProfilePatternKeyExists()
        {
            string fundingLineCode = NewRandomString();
            string key = NewRandomString();

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(key));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv => ppv.WithProfilePatternKeys(
                    NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineCode).WithKey(key))))));
            GivenGetPublishedProvider(publishedProvider);

            StatusCodeResult result = await WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey) as StatusCodeResult;

            result
                .Should()
                .NotBeNull();

            result
                .StatusCode
                .Should()
                .Be((int)HttpStatusCode.NotModified);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsBadRequestIfNewPublishedProviderVersionCreationFailed()
        {
            string fundingLineCode = NewRandomString();
            string existingProfilePatternKey = NewRandomString();
            string newProfilePatterFundingKey = NewRandomString();

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));

            FundingLine fundingLine = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCode));
            PublishedProviderVersion existingPublishedProviderVersion =
                NewPublishedProviderVersion(ppv => ppv
                    .WithFundingStreamId(_fundingStreamId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(existingProfilePatternKey)))
                    .WithFundingLines(new[] { fundingLine })
            );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));

            GivenGetPublishedProvider(publishedProvider);

            IActionResult result = await WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultReturnedAs(result, HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyReturnsOkAndUpdatesPatternKeyWithNewVersion()
        {
            string fundingLineCode = NewRandomString();
            string existingProfilePatternKey = NewRandomString();
            string newProfilePatterFundingKey = NewRandomString();
            string specificationId = NewRandomString();

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));

            FundingLine fundingLine = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCode));
            PublishedProviderVersion existingPublishedProviderVersion =
                NewPublishedProviderVersion(ppv => ppv
                    .WithFundingStreamId(_fundingStreamId)
                    .WithSpecificationId(specificationId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(existingProfilePatternKey)))
                    .WithFundingLines(new[] { fundingLine })
            );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));
            PublishedProviderVersion newPublishedProviderVersion = existingPublishedProviderVersion;
            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest =
                NewPublishedProviderCreateVersionRequest(_ => _
                    .WithPublishedProvider(publishedProvider)
                    .WithNewVersion(newPublishedProviderVersion));

            GivenGetPublishedProvider(publishedProvider);
            AndAssemblePublishedProviderCreateVersionRequests(publishedProvider, publishedProviderCreateVersionRequest);
            AndCreateVersion(publishedProvider, publishedProviderCreateVersionRequest);
            AndGetProfileVariationPointers(null, specificationId);
            AndProfileFundingLines(profilePatternKey);
            AndSaveVersion(HttpStatusCode.OK, existingPublishedProviderVersion);
            AndUpsertPublishedProvider(HttpStatusCode.OK, publishedProvider);

            IActionResult result = await WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultReturnedAs(result, HttpStatusCode.OK);
            AndProfilePatternKeyUpdated(newPublishedProviderVersion, profilePatternKey);
            AndPublishedProviderProcessed(publishedProvider);
            AndProfilingAuditUpdatedForFundingLine(publishedProvider, profilePatternKey.FundingLineCode, _author);
        }

        [TestMethod]
        public async Task AssignProfilePatternKeyForFundingLineWithAlreadyPaidProfilePeriodsOnlyUpdatesUnpaidProfilePeriods()
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

            ProfilePatternKey profilePatternKey = NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(newProfilePatterFundingKey));
            ProfilePeriod paidProfilePeriod = NewProfilePeriod(pp => pp
                                .WithDistributionPeriodId(distributionPeriodId)
                                .WithType(profilePeriodType)
                                .WithTypeValue(typeValue)
                                .WithYear(year)
                                .WithOccurence(occurence));

            FundingLine fundingLine = NewFundingLine(_ => _
                .WithFundingLineCode(fundingLineCode)
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
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(_ => _.WithFundingLineCode(fundingLineCode).WithKey(existingProfilePatternKey)))
                    .WithFundingLines(new[] { fundingLine })
            );

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(existingPublishedProviderVersion));
            PublishedProviderVersion newPublishedProviderVersion = existingPublishedProviderVersion;
            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = NewPublishedProviderCreateVersionRequest(_ =>
                _.WithPublishedProvider(publishedProvider).WithNewVersion(newPublishedProviderVersion));
            IEnumerable<ProfileVariationPointer> profileVariationPointers =
                NewProfileVariationPointers(_ => _
                .WithFundingLineId(fundingLineCode)
                .WithFundingStreamId(_fundingStreamId)
                .WithOccurence(occurence)
                .WithPeriodType(profilePeriodType.ToString())
                .WithTypeValue(typeValue)
                .WithYear(year));

            GivenGetPublishedProvider(publishedProvider);
            AndAssemblePublishedProviderCreateVersionRequests(publishedProvider, publishedProviderCreateVersionRequest);
            AndCreateVersion(publishedProvider, publishedProviderCreateVersionRequest);
            AndGetProfileVariationPointers(profileVariationPointers, specificationId);
            AndProfileFundingLines(profilePatternKey);
            AndSaveVersion(HttpStatusCode.OK, existingPublishedProviderVersion);
            AndUpsertPublishedProvider(HttpStatusCode.OK, publishedProvider);

            IActionResult result = await WhenAssigningProfilePatternKey(_fundingStreamId, _fundingPeriodId, _providerId, profilePatternKey);

            ThenResultReturnedAs(result, HttpStatusCode.OK);
            AndProfilePatternKeyUpdated(newPublishedProviderVersion, profilePatternKey);
            AndPaidProfilePeriodExists(distributionPeriodId, paidProfilePeriod, profilePatternKey);
            AndPublishedProviderProcessed(publishedProvider);
            AndProfilingAuditUpdatedForFundingLine(publishedProvider, profilePatternKey.FundingLineCode, _author);
        }

        private void GivenGetPublishedProvider(PublishedProvider publishedProvider)
        {
            _publishedFundingRepository
                .GetPublishedProvider(_fundingStreamId, _fundingPeriodId, _providerId)
                .Returns(publishedProvider);
        }

        private void AndAssemblePublishedProviderCreateVersionRequests(PublishedProvider expectedPublishedProvider, PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest)
        {
            _publishedProviderVersioningService
                .AssemblePublishedProviderCreateVersionRequests(
                    Arg.Is<IEnumerable<PublishedProvider>>(_ => PublishedProviderMatches(_, expectedPublishedProvider)),
                    Arg.Is<Reference>(_ => _ != null && _author != null && _.ToString() == _author.ToString()),
                    PublishedProviderStatus.Updated)
                .Returns(new[] { publishedProviderCreateVersionRequest });
        }

        private void AndCreateVersion(PublishedProvider publishedProvider, PublishedProviderCreateVersionRequest expectedPublishedProviderCreateVersionRequest)
        {
            _publishedProviderVersioningService
                .CreateVersion(Arg.Is(expectedPublishedProviderCreateVersionRequest))
                .Returns(Task.FromResult(publishedProvider));
        }

        private void AndGetProfileVariationPointers(IEnumerable<ProfileVariationPointer> profileVariationPointers, string expectedSpecificationId)
        {
            _specificationsApiClient
                .GetProfileVariationPointers(expectedSpecificationId)
                .Returns(Task.FromResult(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, profileVariationPointers)));
        }

        private void AndProfileFundingLines(ProfilePatternKey profilePatternKey)
        {
            _profilingService
               .ProfileFundingLines(
                    Arg.Is<IEnumerable<FundingLine>>(_ =>
                        FundingLinesMatches(_)),
                        _fundingStreamId,
                        _fundingPeriodId,
                        Arg.Is<IEnumerable<ProfilePatternKey>>(_ => _.Any(k => k.FundingLineCode == profilePatternKey.FundingLineCode && k.Key == profilePatternKey.Key)))
               .Returns(_ =>
               {
                   IEnumerable<FundingLine> fundingLines = _.Arg<IEnumerable<FundingLine>>();
                   FundingLine fundingLine = fundingLines.FirstOrDefault();

                   if (fundingLine.DistributionPeriods == null)
                   {
                       fundingLine.DistributionPeriods = new List<DistributionPeriod>();
                   }

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

                   return Task.CompletedTask;
               });
        }

        private void AndSaveVersion(HttpStatusCode httpStatusCode, PublishedProviderVersion expectedPublishedProviderVersion)
        {
            _publishedProviderVersioningService
                .SaveVersion(expectedPublishedProviderVersion)
                .Returns(Task.FromResult(httpStatusCode));
        }

        private void AndUpsertPublishedProvider(HttpStatusCode httpStatusCode, PublishedProvider expectedPublishedProvider)
        {
            _publishedFundingRepository
                .UpsertPublishedProvider(expectedPublishedProvider)
                .Returns(Task.FromResult(httpStatusCode));
        }

        private async Task<IActionResult> WhenAssigningProfilePatternKey(string fundingStreamId, string fundingPeriodId, string providerId, ProfilePatternKey profilePatternKey)
        {
            return await _service.AssignProfilePatternKey(fundingStreamId, fundingPeriodId, providerId, profilePatternKey, _author);
        }

        private void ThenResultReturnedAs(IActionResult actionResult, HttpStatusCode httpStatusCode)
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
                .Be((int)httpStatusCode);
        }

        private void AndProfilePatternKeyUpdated(PublishedProviderVersion publishedProviderVersion, ProfilePatternKey profilePatternKey)
        {
            Assert.AreEqual(publishedProviderVersion.ProfilePatternKeys.SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode), profilePatternKey);
        }

        private void AndPublishedProviderProcessed(PublishedProvider publishedProvider)
        {
            _publishedProviderErrorDetection
                .Received(1)
                .ProcessPublishedProvider(publishedProvider, Arg.Any<Func<IDetectPublishedProviderErrors, bool>>(), Arg.Any<PublishedProvidersContext>());
        }

        private void AndProfilingAuditUpdatedForFundingLine(PublishedProvider publishedProvider, string fundingLineCode, Reference author)
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

        private bool PublishedProviderMatches(IEnumerable<PublishedProvider> publishedProviders, PublishedProvider expectedPublishedProvider)
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

            ProfilePeriod firstProfilePeriod = fundingLine.DistributionPeriods.SingleOrDefault(_ => _.DistributionPeriodId == _distributionPeriod1Id)?.ProfilePeriods.FirstOrDefault();
            ProfilePeriod lastProfilePeriod = fundingLine.DistributionPeriods.SingleOrDefault(_ => _.DistributionPeriodId == _distributionPeriod2Id)?.ProfilePeriods.FirstOrDefault();

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

        private void AndPaidProfilePeriodExists(string distributionId, ProfilePeriod profilePeriod, ProfilePatternKey profilePatternKey)
        {
            _profilingService
                .Received(1)
                .ProfileFundingLines(Arg.Is<IEnumerable<FundingLine>>(_ =>
                    PaidProfileFundingLinesMatches(_, distributionId, profilePeriod)),
                    _fundingStreamId,
                    _fundingPeriodId,
                    Arg.Is<IEnumerable<ProfilePatternKey>>(_ => _.Any(k => k.FundingLineCode == profilePatternKey.FundingLineCode && k.Key == profilePatternKey.Key)));
        }

        private bool PaidProfileFundingLinesMatches(IEnumerable<FundingLine> fundingLines, string distributionId, ProfilePeriod profilePeriod)
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

        private static IEnumerable<ProfilePeriod> NewProfilePeriods(params Action<ProfilePeriodBuilder>[] setUps)
        {
            return setUps.Select(NewProfilePeriod);
        }

        private static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        private static IEnumerable<DistributionPeriod> NewDistributionPeriods(params Action<DistributionPeriodBuilder>[] setUps)
        {
            return setUps.Select(NewDistributionPeriod);
        }

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

        private static IEnumerable<ProfileVariationPointer> NewProfileVariationPointers(params Action<ProfileVariationPointerBuilder>[] setUps)
        {
            return setUps.Select(NewProfileVariationPointer);
        }

        private string NewRandomString() => new RandomString();
        private int NewRandomNumberBetween(int min, int max) => new RandomNumberBetween(min, max);
        private static TEnum NewRandomEnum<TEnum>() where TEnum : struct => new RandomEnum<TEnum>();
    }
}
