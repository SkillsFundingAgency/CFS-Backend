using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ChannelOrganisationGroupChangeDetectorTests
    {
        private ChannelOrganisationGroupChangeDetector _service;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IPublishedProvidersLoadContext> _publishedProvidersLoadContext;
        private Mock<IMapper> _mapper;

        private SpecificationSummary _specificationSummary;
        private Channel _channel;

        [TestInitialize]
        public void Initialise()
        {
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _publishedProvidersLoadContext = new Mock<IPublishedProvidersLoadContext>();
            _mapper = new Mock<IMapper>();

            _service = new ChannelOrganisationGroupChangeDetector(_releaseManagementRepository.Object, _publishedProvidersLoadContext.Object, _mapper.Object);

            _specificationSummary = NewSpecificationSummary();
            _channel = NewChannel();
        }

        [TestMethod]
        public async Task WhenDetermineFundingGroupsToCreateBasedOnProviderVersions_OnlyReleasesProvidersInApprovedState()
        {
            OrganisationGroupingReason organisationGroupingReasonOne = OrganisationGroupingReason.Information;
            OrganisationGroupTypeCode organisationGroupTypeCodeOne = OrganisationGroupTypeCode.AcademyTrust;

            string organisationGroupIdentifierValueOne = NewRandomString();
            string organisationGroupIdentifierValueTwo = NewRandomString();

            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();

            int majorVersionOne = NewRandomNumber();

            string organisationGroupResultNameOne = NewRandomString();
            string organisationGroupResultNameTwo = NewRandomString();
            string organisationGroupResultNameThree = NewRandomString();
            string organisationGroupResultNameFour = NewRandomString();

            IEnumerable<OrganisationGroupResult> organisationGroupResults = new List<OrganisationGroupResult>
            {
                NewOrganisationGroupResult(_ => _
                    .WithGroupReason(organisationGroupingReasonOne)
                    .WithGroupTypeCode(organisationGroupTypeCodeOne)
                    .WithIdentifierValue(organisationGroupIdentifierValueTwo)
                    .WithName(organisationGroupResultNameOne)),
                NewOrganisationGroupResult(_ => _
                    .WithGroupReason(organisationGroupingReasonOne)
                    .WithGroupTypeCode(organisationGroupTypeCodeOne)
                    .WithIdentifierValue(organisationGroupIdentifierValueOne)
                    .WithName(organisationGroupResultNameTwo)
                    .WithProviders(
                        NewApiProviders( 
                            ap => ap.WithProviderId(providerIdOne)))),
                NewOrganisationGroupResult(_ => _
                    .WithGroupReason(organisationGroupingReasonOne)
                    .WithGroupTypeCode(organisationGroupTypeCodeOne)
                    .WithIdentifierValue(organisationGroupIdentifierValueOne)
                    .WithName(organisationGroupResultNameThree)
                    .WithProviders(
                        NewApiProviders(
                            ap => ap.WithProviderId(providerIdOne)))),
                NewOrganisationGroupResult(_ => _
                    .WithGroupReason(organisationGroupingReasonOne)
                    .WithGroupTypeCode(organisationGroupTypeCodeOne)
                    .WithIdentifierValue(organisationGroupIdentifierValueOne)
                    .WithName(organisationGroupResultNameFour)
                    .WithProviders(
                        NewApiProviders(
                            ap => ap.WithProviderId(providerIdTwo))))
            };

            IEnumerable<LatestProviderVersionInFundingGroup> latestProviderVersionInFundingGroups = new List<LatestProviderVersionInFundingGroup>
            {
                NewLatestProviderVersionInFundingGroup(_ => _
                    .WithGroupingReasonCode(organisationGroupingReasonOne.ToString())
                    .WithOrganisationGroupTypeCode(organisationGroupTypeCodeOne.ToString())
                    .WithOrganisationGroupIdentifierValue(organisationGroupIdentifierValueOne)
                    .WithProviderId(providerIdOne)
                    .WithMajorVersion(majorVersionOne))
            };

            IEnumerable<PublishedProvider> publishedProviders = NewPublishedProviders(
                pp => pp
                    .WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerIdOne)))
                    .WithReleased(NewPublishedProviderVersion(ppv => ppv.WithMajorVersion(majorVersionOne))));

            GivenLatestProviderVersionInFundingGroups(latestProviderVersionInFundingGroups);
            AndGetOrLoadProviders(publishedProviders);

            (IEnumerable<OrganisationGroupResult> results, Dictionary<string, PublishedProviderVersion> providersInGroupsToCreate)
                = await WhenDetermineFundingGroupsToCreateBasedOnProviderVersions(organisationGroupResults, 
                publishedProviders.Select(_ => _.Current).ToDictionary(_ => _.ProviderId),
                _specificationSummary, 
                _channel);

            results.Count().Should().Be(2);
            results.FirstOrDefault().Name.Should().Be(organisationGroupResultNameOne);
            results.LastOrDefault().Name.Should().Be(organisationGroupResultNameFour);
        }

        private void GivenLatestProviderVersionInFundingGroups(
            IEnumerable<LatestProviderVersionInFundingGroup> latestProviderVersionInFundingGroups)
        {
            _releaseManagementRepository
                .Setup(_ => _.GetLatestProviderVersionInFundingGroups(_specificationSummary.Id, _channel.ChannelId))
                .ReturnsAsync(latestProviderVersionInFundingGroups);
        }

        private void AndGetOrLoadProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProvidersLoadContext
                .Setup(_ => _.GetOrLoadProviders(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(publishedProviders);
        }

        private async Task<(IEnumerable<OrganisationGroupResult>, Dictionary<string, PublishedProviderVersion>)> WhenDetermineFundingGroupsToCreateBasedOnProviderVersions(
            IEnumerable<OrganisationGroupResult> channelOrganisationGroups,
            Dictionary<string, PublishedProviderVersion> providersToRelease,
            SpecificationSummary specificationSummary,
            Channel channel)
        {
            return await _service.DetermineFundingGroupsToCreateBasedOnProviderVersions(
                channelOrganisationGroups,
                providersToRelease,
                specificationSummary,
                channel
                );
        }

        

        private static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setup = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setup?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setup?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private static Channel NewChannel(Action<ChannelBuilder> setup = null)
        {
            ChannelBuilder channelBuilder = new ChannelBuilder();

            setup?.Invoke(channelBuilder);

            return channelBuilder.Build();
        }

        private static ApiProvider NewApiProvider(Action<ApiProviderBuilder> setup = null)
        {
            ApiProviderBuilder providerBuilder = new ApiProviderBuilder();

            setup?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected static IEnumerable<ApiProvider> NewApiProviders(params Action<ApiProviderBuilder>[] setUps)
            => setUps.Select(NewApiProvider);

        private static LatestProviderVersionInFundingGroup NewLatestProviderVersionInFundingGroup(Action<LatestProviderVersionInFundingGroupBuilder> setup = null)
        {
            LatestProviderVersionInFundingGroupBuilder latestProviderVersionInFundingGroupBuilder = new LatestProviderVersionInFundingGroupBuilder();

            setup?.Invoke(latestProviderVersionInFundingGroupBuilder);

            return latestProviderVersionInFundingGroupBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setup = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setup?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setup = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setup?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        protected static IEnumerable<PublishedProvider> NewPublishedProviders(params Action<PublishedProviderBuilder>[] setUps)
            => setUps.Select(NewPublishedProvider);


        private static string NewRandomString() => new RandomString();
        private static int NewRandomNumber() => new RandomNumberBetween(1, 100);

    }
}
