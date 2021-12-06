using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedProvidersSearchServiceTests
    {
        private Mock<IReleaseManagementRepository> _repo;
        private Mock<IPoliciesService> _policiesService;
        private PublishedProvidersSearchService _sut;
        private string _specificationOneId;
        private string _specificationTwoId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _channelCodeOne;
        private string _channelCodeTwo;
        private string _providerIdOne;
        private string _providerIdTwo;
        private const int _channelIdOne = 1;
        private const int _channelIdTwo = 2;
        private const int _publishedProviderOneMajorVersion = 1;
        private const int _publishedProviderTwoMajorVersion = 2;
        private const int _publishedProviderOneMinorVersion = 1;
        private const int _publishedProviderTwoMinorVersion = 2;

        [TestInitialize]
        public void Initialize()
        {
            _repo = new Mock<IReleaseManagementRepository>();
            _policiesService = new Mock<IPoliciesService>();
            _specificationOneId = new RandomString();
            _specificationTwoId = new RandomString();
            _fundingStreamId = new RandomString();
            _fundingPeriodId = new RandomString();
            _channelCodeOne = new RandomString();
            _channelCodeTwo = new RandomString();
            _providerIdOne = new RandomString();
            _providerIdTwo = new RandomString();

            _repo.Setup(_ => _.GetChannelByChannelCode(It.Is<string>(s => s == _channelCodeOne)))
                .ReturnsAsync(new Channel { ChannelCode = _channelCodeOne, ChannelId = _channelIdOne, ChannelName = _channelCodeOne });
            _repo.Setup(_ => _.GetChannelByChannelCode(It.Is<string>(s => s == _channelCodeTwo)))
                .ReturnsAsync(new Channel { ChannelCode = _channelCodeTwo, ChannelId = _channelIdTwo, ChannelName = _channelCodeTwo });

            _sut = new PublishedProvidersSearchService(_repo.Object, _policiesService.Object);
        }

        [TestMethod]
        public async Task ReturnsReleaseChannelsForVisibleChannelsOnly_OneVisible()
        {
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeOne)
                        .WithIsVisible(true)),
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeTwo)
                        .WithIsVisible(false))));

            GivenFundingConfiguration(fundingConfiguration);

            GivenLatestProviderVersionInChannels(
                _specificationOneId,
                fundingConfiguration,
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeOne)
                    .WithChannelName(_channelCodeOne)
                    .WithChannelId(_channelIdOne)
                    .WithMajorVersion(_publishedProviderOneMajorVersion)
                    .WithMinorVersion(_publishedProviderOneMinorVersion)
                    .WithProviderId(_providerIdOne)),
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeTwo)
                    .WithChannelName(_channelCodeTwo)
                    .WithChannelId(_channelIdTwo)
                    .WithMajorVersion(_publishedProviderTwoMajorVersion)
                    .WithMinorVersion(_publishedProviderTwoMinorVersion)
                    .WithProviderId(_providerIdTwo)));

            List<ReleaseChannelSearch> request = NewRequest(NewReleaseChannelSearch(_specificationOneId, _fundingStreamId, _fundingPeriodId));

            Dictionary<string, IEnumerable<ReleaseChannel>> result = await _sut.GetPublishedProviderReleaseChannelsLookup(request);

            result
                .Should()
                .NotBeNull();

            result
                .Should()
                .HaveCount(fundingConfiguration.ReleaseChannels.Count(_ => _.IsVisible));

            result[_providerIdOne]
                .Should()
                .BeEquivalentTo(new List<ReleaseChannel>
                {
                    new ReleaseChannel
                    {
                        ChannelCode = _channelCodeOne,
                        ChannelName = _channelCodeOne,
                        MajorVersion = _publishedProviderOneMajorVersion,
                        MinorVersion = _publishedProviderOneMinorVersion
                    }
                });
        }

        [TestMethod]
        public async Task ReturnsReleaseChannelsForVisibleChannelsOnly_BothVisible()
        {
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeOne)
                        .WithIsVisible(true)),
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeTwo)
                        .WithIsVisible(true))));

            GivenFundingConfiguration(fundingConfiguration);

            GivenLatestProviderVersionInChannels(
                _specificationOneId,
                fundingConfiguration,
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeOne)
                    .WithChannelName(_channelCodeOne)
                    .WithChannelId(_channelIdOne)
                    .WithMajorVersion(_publishedProviderOneMajorVersion)
                    .WithMinorVersion(_publishedProviderOneMinorVersion)
                    .WithProviderId(_providerIdOne)),
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeTwo)
                    .WithChannelName(_channelCodeTwo)
                    .WithChannelId(_channelIdTwo)
                    .WithMajorVersion(_publishedProviderTwoMajorVersion)
                    .WithMinorVersion(_publishedProviderTwoMinorVersion)
                    .WithProviderId(_providerIdTwo)));

            List<ReleaseChannelSearch> request = NewRequest(NewReleaseChannelSearch(_specificationOneId, _fundingStreamId, _fundingPeriodId));

            Dictionary<string, IEnumerable<ReleaseChannel>> result = await _sut.GetPublishedProviderReleaseChannelsLookup(request);

            result
                .Should()
                .NotBeNull();

            result
                .Should()
                .HaveCount(fundingConfiguration.ReleaseChannels.Count(_ => _.IsVisible));

            result[_providerIdOne]
                .Should()
                .BeEquivalentTo(new List<ReleaseChannel>
                {
                    new ReleaseChannel
                    {
                        ChannelCode = _channelCodeOne,
                        ChannelName = _channelCodeOne,
                        MajorVersion = _publishedProviderOneMajorVersion,
                        MinorVersion = _publishedProviderOneMinorVersion
                    }
                });

            result[_providerIdTwo]
                .Should()
                .BeEquivalentTo(new List<ReleaseChannel>
                {
                    new ReleaseChannel
                    {
                        ChannelCode = _channelCodeTwo,
                        ChannelName = _channelCodeTwo,
                        MajorVersion = _publishedProviderTwoMajorVersion,
                        MinorVersion = _publishedProviderTwoMinorVersion
                    }
                });
        }

        [TestMethod]
        public async Task HandlesNoChannels()
        {
            FundingConfiguration fundingConfiguration = NewFundingConfiguration();

            GivenFundingConfiguration(fundingConfiguration);

            List<ReleaseChannelSearch> request = NewRequest(NewReleaseChannelSearch(_specificationOneId, _fundingStreamId, _fundingPeriodId));

            Dictionary<string, IEnumerable<ReleaseChannel>> result = await _sut.GetPublishedProviderReleaseChannelsLookup(request);

            result
                .Should()
                .NotBeNull();

            result
                .Should()
                .HaveCount(fundingConfiguration.ReleaseChannels.Count(_ => _.IsVisible));

            _repo.Verify(_ => _.GetChannelByChannelCode(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ReturnsDistinctReleaseChannelsWhenMultipleSpecifications()
        {
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeOne)
                        .WithIsVisible(true)),
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(_channelCodeTwo)
                        .WithIsVisible(true))));

            GivenFundingConfiguration(fundingConfiguration);

            GivenLatestProviderVersionInChannels(
                _specificationOneId,
                fundingConfiguration,
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeOne)
                    .WithChannelName(_channelCodeOne)
                    .WithChannelId(_channelIdOne)
                    .WithMajorVersion(_publishedProviderOneMajorVersion)
                    .WithMinorVersion(_publishedProviderOneMinorVersion)
                    .WithProviderId(_providerIdOne)),
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeTwo)
                    .WithChannelName(_channelCodeTwo)
                    .WithChannelId(_channelIdTwo)
                    .WithMajorVersion(_publishedProviderTwoMajorVersion)
                    .WithMinorVersion(_publishedProviderTwoMinorVersion)
                    .WithProviderId(_providerIdTwo)));

            GivenLatestProviderVersionInChannels(
                _specificationTwoId,
                fundingConfiguration,
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeOne)
                    .WithChannelName(_channelCodeOne)
                    .WithChannelId(_channelIdOne)
                    .WithMajorVersion(_publishedProviderOneMajorVersion)
                    .WithMinorVersion(_publishedProviderOneMinorVersion)
                    .WithProviderId(_providerIdOne)),
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeTwo)
                    .WithChannelName(_channelCodeTwo)
                    .WithChannelId(_channelIdTwo)
                    .WithMajorVersion(_publishedProviderTwoMajorVersion)
                    .WithMinorVersion(_publishedProviderTwoMinorVersion)
                    .WithProviderId(_providerIdTwo)));

            List<ReleaseChannelSearch> request = NewRequest(
                NewReleaseChannelSearch(_specificationOneId, _fundingStreamId, _fundingPeriodId),
                NewReleaseChannelSearch(_specificationTwoId, _fundingStreamId, _fundingPeriodId));

            Dictionary<string, IEnumerable<ReleaseChannel>> result = await _sut.GetPublishedProviderReleaseChannelsLookup(request);

            result
                .Should()
                .NotBeNull();

            result
                .Should()
                .HaveCount(fundingConfiguration.ReleaseChannels.Count(_ => _.IsVisible));

            result[_providerIdOne]
                .Should()
                .BeEquivalentTo(new List<ReleaseChannel>
                {
                    new ReleaseChannel
                    {
                        ChannelCode = _channelCodeOne,
                        ChannelName = _channelCodeOne,
                        MajorVersion = _publishedProviderOneMajorVersion,
                        MinorVersion = _publishedProviderOneMinorVersion
                    }
                });

            result[_providerIdTwo]
                .Should()
                .BeEquivalentTo(new List<ReleaseChannel>
                {
                    new ReleaseChannel
                    {
                        ChannelCode = _channelCodeTwo,
                        ChannelName = _channelCodeTwo,
                        MajorVersion = _publishedProviderTwoMajorVersion,
                        MinorVersion = _publishedProviderTwoMinorVersion
                    }
                });
        }

        [TestMethod]
        public void ThrowsKeyNotFoundExceptionIfChannelCodeNotFound()
        {
            string unknownChannelCode = new RandomString();

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(_ => _
                        .WithChannelCode(unknownChannelCode)
                        .WithIsVisible(true))));

            GivenFundingConfiguration(fundingConfiguration);

            GivenLatestProviderVersionInChannels(
                _specificationOneId,
                fundingConfiguration,
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeOne)
                    .WithChannelName(_channelCodeOne)
                    .WithChannelId(_channelIdOne)
                    .WithMajorVersion(_publishedProviderOneMajorVersion)
                    .WithMinorVersion(_publishedProviderOneMinorVersion)
                    .WithProviderId(_providerIdOne)),
                NewProviderVersionInChannel(_ => _
                    .WithChannelCode(_channelCodeTwo)
                    .WithChannelName(_channelCodeTwo)
                    .WithChannelId(_channelIdTwo)
                    .WithMajorVersion(_publishedProviderTwoMajorVersion)
                    .WithMinorVersion(_publishedProviderTwoMinorVersion)
                    .WithProviderId(_providerIdTwo)));

            List<ReleaseChannelSearch> request = NewRequest(NewReleaseChannelSearch(_specificationOneId, _fundingStreamId, _fundingPeriodId));

            Func<Task> result = async () => await _sut.GetPublishedProviderReleaseChannelsLookup(request);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .WithMessage($"PublishedProvidersSearchService:GetVisibleChannelIds ChannelCode {unknownChannelCode} could not be found." +
                    $"FundingStreamId: {_fundingStreamId} and FundingPeriodId: {_fundingPeriodId}");
        }

        private void GivenLatestProviderVersionInChannels(string specificationId, FundingConfiguration fundingConfiguration, params ProviderVersionInChannel[] providerVersionInChannels)
        {
            IEnumerable<string> visibleChannels = fundingConfiguration.ReleaseChannels.Where(_ => _.IsVisible).Select(_ => _.ChannelCode);
            _repo.Setup(_ => _.GetLatestPublishedProviderVersions(It.Is<string>(s => s == specificationId), It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(providerVersionInChannels.Where(_ => visibleChannels.Contains(_.ChannelCode)));
        }

        private void GivenFundingConfiguration(FundingConfiguration fundingConfiguration)
        {
            _policiesService.Setup(_ => _.GetFundingConfiguration(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fundingConfiguration);
        }

        private List<ReleaseChannelSearch> NewRequest(params ReleaseChannelSearch[] releaseChannelSearches)
        {
            return releaseChannelSearches.ToList();
        }

        private ReleaseChannelSearch NewReleaseChannelSearch(string specificationId, string fundingStreamId, string fundingPeriodId)
        {
            return new ReleaseChannelSearch
            {
                SpecificationId = specificationId,
                FundingStreamId = fundingStreamId,
                FundingPeriodId = fundingPeriodId
            };
        }

        private ProviderVersionInChannel NewProviderVersionInChannel(Action<ProviderVersionInChannelBuilder> setup = null)
        {
            ProviderVersionInChannelBuilder builder = new ProviderVersionInChannelBuilder();

            setup?.Invoke(builder);

            return builder.Build();
        }

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder builder = new FundingConfigurationBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private FundingConfigurationChannel NewFundingConfigurationChannel(Action<FundingConfigurationChannelBuilder> setUp = null)
        {
            FundingConfigurationChannelBuilder builder = new FundingConfigurationChannelBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}
