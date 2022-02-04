﻿using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ProviderVersionToChannelReleaseServiceTests
    {
        private const int channelId = 1;
        private readonly DateTime statusDateTime = DateTime.UtcNow;
        private Reference _author;
        private ProviderVersionToChannelReleaseService _service;
        private Dictionary<string, ReleasedProviderVersion> _providerVersions;
        private Dictionary<string, ReleasedProviderVersionChannel> _providerVersionChannels;
        private Dictionary<int, Dictionary<string, FundingGroupVersion>> _fundingGroupVersions;
        private List<ReleasedProvider> _releasedProviders;
        private IEnumerable<string> _releasedProviderIds;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Initialise()
        {
            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _logger = new Mock<ILogger>();
            _author = new Reference("author", "author");

            _releaseToChannelSqlMappingContext.SetupGet(_ => _.Author)
                .Returns(_author);

            _service = new ProviderVersionToChannelReleaseService(
                _releaseManagementRepository.Object, _releaseToChannelSqlMappingContext.Object, _logger.Object);

            _releasedProviders = new List<ReleasedProvider>
            {
                new ReleasedProvider
                {
                    ProviderId = new RandomString(),
                },
                new ReleasedProvider
                {
                    ProviderId = new RandomString(),
                }
            };

            _releasedProviderIds = _releasedProviders.Select(_ => _.ProviderId);

            int id = 1;

            _providerVersions = _releasedProviders.ToDictionary(_ => _.ProviderId, _ => new ReleasedProviderVersion
            {
                ReleasedProviderVersionId = id++
            });

            id = 1;

            _providerVersionChannels = _releasedProviders.ToDictionary(_ => $"{_.ProviderId}_{channelId}", _ => new ReleasedProviderVersionChannel
            {
                ChannelId = channelId,
                ReleasedProviderVersionId = id++,
                AuthorId = _author.Id,
                AuthorName = _author.Name,
                StatusChangedDate = statusDateTime
            });

            _fundingGroupVersions = new Dictionary<int, Dictionary<string, FundingGroupVersion>>()
            {
                { 1,  _releasedProviders.ToDictionary(_ => _.ProviderId, _ => new FundingGroupVersion
                        {
                            FundingGroupVersionId = new RandomNumberBetween(1, 10)
                        })
                }
            };
        }

        [TestMethod]
        public async Task WhenGivenProviderVersionChannelsNotInContextThenTheyAreSuccessfullyReleased()
        {
            GivenContext(null, _providerVersions, _fundingGroupVersions);
            GivenReleasedProviderVersionChannel();

            await _service.ReleaseProviderVersionChannel(_releasedProviderIds, channelId, statusDateTime);

            _releaseManagementRepository.Verify(
                r => r.CreateReleasedProviderVersionChannelsUsingAmbientTransaction(It.IsAny<ReleasedProviderVersionChannel>()), Times.Exactly(_releasedProviders.Count));
        }

        [TestMethod]
        public async Task WhenGivenProviderVersionChannelsInContextThenTheyAreNotReleased()
        {
            GivenContext(_providerVersionChannels, _providerVersions, null);

            await _service.ReleaseProviderVersionChannel(_releasedProviderIds, channelId, statusDateTime);

            _releaseManagementRepository.Verify(
                r => r.CreateReleasedProviderVersionChannelsUsingAmbientTransaction(It.IsAny<ReleasedProviderVersionChannel>()), Times.Never);
        }

        [TestMethod]
        public void WhenGivenProviderVersionNotFoundInContext_Throws()
        {
            GivenContext(null, null, null);

            Func<Task> result = async () => await _service.ReleaseProviderVersionChannel(_releasedProviderIds, channelId, statusDateTime);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();

            _releaseManagementRepository.Verify(
                r => r.CreateReleasedProviderVersionChannelsUsingAmbientTransaction(It.IsAny<ReleasedProviderVersionChannel>()), Times.Never);
        }

        private void GivenReleasedProviderVersionChannel()
        {
            _releaseManagementRepository
                .Setup(s => s.CreateReleasedProviderVersionChannelsUsingAmbientTransaction(It.IsAny<ReleasedProviderVersionChannel>()))
                .ReturnsAsync(new ReleasedProviderVersionChannel
                {
                    ReleasedProviderVersionId = new RandomNumberBetween(1, 10),
                    ChannelId = new RandomNumberBetween(1, 10),
                    StatusChangedDate = DateTime.UtcNow,
                    AuthorId = Guid.NewGuid().ToString(),
                    AuthorName = Guid.NewGuid().ToString()
                });
        }

        private void GivenContext(
            Dictionary<string, ReleasedProviderVersionChannel> providerVersionChannels,
            Dictionary<string, ReleasedProviderVersion> providerVersions,
            Dictionary<int, Dictionary<string, FundingGroupVersion>> fundingGroupVersions)
        {
            if (providerVersionChannels == null)
            {
                providerVersionChannels = new Dictionary<string, ReleasedProviderVersionChannel>();
            }

            if (providerVersions == null)
            {
                providerVersions = new Dictionary<string, ReleasedProviderVersion>();
            }

            if (fundingGroupVersions == null)
            {
                fundingGroupVersions = new Dictionary<int, Dictionary<string, FundingGroupVersion>>();
            }

            _releaseToChannelSqlMappingContext.SetupGet(s => s.ReleasedProviderVersionChannels)
                .Returns(providerVersionChannels);

            _releaseToChannelSqlMappingContext.SetupGet(s => s.ReleasedProviderVersions)
                .Returns(providerVersions);

            _releaseToChannelSqlMappingContext.SetupGet(s => s.FundingGroupVersions)
                .Returns(fundingGroupVersions);
        }
    }
}
