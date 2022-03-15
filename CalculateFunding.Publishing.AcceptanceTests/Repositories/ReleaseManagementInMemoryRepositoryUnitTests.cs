using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    [TestClass]
    public class ReleaseManagementInMemoryRepositoryUnitTests
    {
        private InMemoryReleaseManagementRepository _repo;
        private int _singleChannelId;
        private int _baseProviderId;
        private int _nextReleasedProviderId;
        private List<ReleasedProvider> _createdReleasedProviders;
        private List<ReleasedProviderVersion> _createdReleasedProviderVersions;
        private List<ProviderVersionInChannel> _generatedProviderVersionInChannel;
        private List<Guid> _generatedProviderVersionInChannelIds;
        private string _specificationId;
        private string _coreProviderVersion;
        private int _fundingPeriodId;
        private int _fundingStreamId;
        private string _authorId;
        private string _authorName;
        private List<ReleasedProviderVersionChannel> _createdReleasedProviderVersionChannels;
        private int _nextChannelId = 1;

        [TestInitialize]
        public void Setup()
        {
            _repo = new InMemoryReleaseManagementRepository();
            _specificationId = new RandomString();
            _singleChannelId = 1;

            _baseProviderId = 100;
            _nextReleasedProviderId = _baseProviderId;

            _coreProviderVersion = new RandomString();

            _createdReleasedProviders = new List<ReleasedProvider>();
            _createdReleasedProviderVersions = new List<ReleasedProviderVersion>();
            _createdReleasedProviderVersionChannels = new List<ReleasedProviderVersionChannel>();

            _generatedProviderVersionInChannel = new List<ProviderVersionInChannel>();
            _generatedProviderVersionInChannelIds = new List<Guid>();

            _authorId = new RandomString();
            _authorName = new RandomString();
        }

        [TestMethod]
        public async Task GivenMultipleChannelsWhenGettingLatestPublishedProviderVersions()
        {

            await AndSpecificationExists();
            await AndReleaseProvidersArePopulated(2);
            await AndReleaseProviderVersionsArePopulated(3, 1);
            await AndReleaseProviderVersionsArePopulated(2, 2);

            await AndChannelExists("Statement", "Statements");

            await AndTheProviderVersionIsReleasedIntoChannel(1, 1, 1);
            await AndTheProviderVersionIsReleasedIntoChannel(1, 2, 1);
            await AndTheProviderVersionIsReleasedIntoChannel(1, 3, 1);
            await AndTheProviderVersionIsReleasedIntoChannel(2, 1, 1);
            await AndTheProviderVersionIsReleasedIntoChannel(2, 2, 1);

            IEnumerable<ProviderVersionInChannel> result = await _repo.GetLatestPublishedProviderVersions(_specificationId, new[] { _singleChannelId });

            List<ProviderVersionInChannel> expected = new List<ProviderVersionInChannel>();

            expected.Add(new ProviderVersionInChannel()
            {
                ChannelCode = "Statement",
                ChannelId = _singleChannelId,
                ChannelName = "Statements",
                CoreProviderVersionId = _coreProviderVersion,
                MajorVersion = 3,
                MinorVersion = 0,
                ProviderId = "100",
                ReleasedProviderVersionChannelId = _createdReleasedProviderVersionChannels[2].ReleasedProviderVersionChannelId,
            });

            expected.Add(new ProviderVersionInChannel()
            {
                ChannelCode = "Statement",
                ChannelId = _singleChannelId,
                ChannelName = "Statements",
                CoreProviderVersionId = _coreProviderVersion,
                MajorVersion = 2,
                MinorVersion = 0,
                ProviderId = "101",
                ReleasedProviderVersionChannelId = _createdReleasedProviderVersionChannels[4].ReleasedProviderVersionChannelId,
            });

            result.OrderByDescending(_ => _.MajorVersion).Should().BeEquivalentTo(expected);

        }

        private async Task AndChannelExists(string v1, string v2)
        {
            Channel channel = new Channel()
            {
                ChannelCode = v1,
                ChannelId = _nextChannelId++,
                ChannelName = v2,
                UrlKey = new RandomString()
            };

            await _repo.CreateChannel(channel);
        }

        private async Task AndSpecificationExists()
        {
            await _repo.CreateSpecification(new Specification()
            {
                FundingPeriodId = _fundingPeriodId,
                FundingStreamId = _fundingStreamId,
                SpecificationId = _specificationId,
                SpecificationName = new RandomString(),
            });
        }

        private async Task AndReleaseProviderVersionsArePopulated(int totalVersionsToCreate, int providerId)
        {
            for (int i = 0; i < totalVersionsToCreate; i++)
            {
                ReleasedProviderVersion releasedProviderVersion = new ReleasedProviderVersion()
                {
                    CoreProviderVersionId = _coreProviderVersion,
                    FundingId = new RandomString(),
                    MajorVersion = i + 1,
                    MinorVersion = 0,
                    ReleasedProviderId = _createdReleasedProviders[providerId - 1].ReleasedProviderId,
                    ReleasedProviderVersionId = Guid.NewGuid(),
                    TotalFunding = new RandomNumberBetween(1, 1000),

                };

                await _repo.CreateReleasedProviderVersion(releasedProviderVersion);

                _createdReleasedProviderVersions.Add(releasedProviderVersion);

            }
        }

        private async Task AndReleaseProvidersArePopulated(int totalReleasedProviders)
        {
            for (int i = 0; i < totalReleasedProviders; i++)
            {
                ReleasedProvider rp = new ReleasedProvider()
                {
                    ProviderId = _nextReleasedProviderId++.ToString(),
                    ReleasedProviderId = Guid.NewGuid(),
                    SpecificationId = _specificationId,
                };

                await _repo.CreateReleasedProvider(rp);

                _createdReleasedProviders.Add(rp);
            }
        }

        private async Task AndTheProviderVersionIsReleasedIntoChannel(int providerId, int providerMajorVersion, int channelId)
        {
            IEnumerable<Channel> channels = await _repo.GetChannels();

            Channel channel = channels.Single(_ => _.ChannelId == channelId);

            var releasedProviderVersions = await _repo.GetReleasedProviderVersions();

            var releasedProviders = await _repo.GetReleasedProviders();

            var releasedProvider = releasedProviders.Single(_ => _.ProviderId == (providerId + _baseProviderId - 1).ToString());

            var releasedProviderVersion = releasedProviderVersions.Single(_ => _.ReleasedProviderId == releasedProvider.ReleasedProviderId && _.MajorVersion == providerMajorVersion);

            ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel()
            {
                AuthorId = _authorId,
                ChannelId = channel.ChannelId,
                AuthorName = _authorName,
                ReleasedProviderVersionChannelId = Guid.NewGuid(),
                ReleasedProviderVersionId = releasedProviderVersion.ReleasedProviderVersionId,
                StatusChangedDate = DateTime.UtcNow,
            };

            _createdReleasedProviderVersionChannels.Add(releasedProviderVersionChannel);


            await _repo.CreateReleasedProviderVersionChannel(releasedProviderVersionChannel);
        }
    }
}
