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
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ProviderVariationReasonsReleaseServiceTests
    {
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<ILogger> _logger;
        private ProviderVariationReasonsReleaseService _service;
        private readonly Channel _channel = new Channel
        {
            ChannelId = 1
        };
        private readonly Dictionary<string, IEnumerable<VariationReason>> _publishedVariationReasons = new Dictionary<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>>
        {
            {
                new RandomString(),
                new List<VariationReason> { VariationReason.AuthorityFieldUpdated, VariationReason.CalculationValuesUpdated }
            },
            {
                new RandomString(),
                new List<VariationReason> { VariationReason.CompaniesHouseNumberFieldUpdated }
            },
        };
        private readonly List<Publishing.FundingManagement.SqlModels.VariationReason> _variationReasons = new List<Publishing.FundingManagement.SqlModels.VariationReason>
        {
            new Publishing.FundingManagement.SqlModels.VariationReason { VariationReasonCode = "AuthorityFieldUpdated" },
            new Publishing.FundingManagement.SqlModels.VariationReason { VariationReasonCode = "CalculationValuesUpdated" },
            new Publishing.FundingManagement.SqlModels.VariationReason { VariationReasonCode = "CompaniesHouseNumberFieldUpdated" },
        };
        private Dictionary<string, ReleasedProviderVersionChannel> _providerVersionChannels;

        [TestInitialize]
        public void Initialise()
        {
            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _logger = new Mock<ILogger>();

            int id = 1;

            _providerVersionChannels = _publishedVariationReasons.ToDictionary(_ => $"{_.Key}_{_channel.ChannelId}", _ => new ReleasedProviderVersionChannel
            {
                ReleasedProviderVersionChannelId = id++,
                ChannelId = _channel.ChannelId
            });

            _releaseManagementRepository.Setup(s => s.CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(It.IsAny<IEnumerable<ReleasedProviderChannelVariationReason>>()))
                .ReturnsAsync(new List<ReleasedProviderChannelVariationReason>());

            _service = new ProviderVariationReasonsReleaseService(
                _releaseToChannelSqlMappingContext.Object, _releaseManagementRepository.Object, _logger.Object);

        }

        [TestMethod]
        public async Task CreatesVariationReasonsSuccessfully()
        {
            GivenVariationReasons(_variationReasons);
            GivenContext(_providerVersionChannels);

            await _service.PopulateReleasedProviderChannelVariationReasons(_publishedVariationReasons, _channel);

            VariationReasonsArePersisted();
        }

        [TestMethod]
        public void WhenKeyMissingFromContext_ThrowsKeyNotFoundException()
        {
            GivenVariationReasons(_variationReasons);
            _providerVersionChannels.Remove(_providerVersionChannels.Keys.First());
            GivenContext(_providerVersionChannels);

            Func<Task> result = async () => await _service.PopulateReleasedProviderChannelVariationReasons(_publishedVariationReasons, _channel);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();

            VariationReasonsAreNotPersisted();
        }

        [TestMethod]
        public void WhenVariationReasonMissingFromDatabase_ThrowsKeyNotFoundException()
        {
            GivenVariationReasons(_variationReasons.Take(1));
            GivenContext(_providerVersionChannels);

            Func<Task> result = async () => await _service.PopulateReleasedProviderChannelVariationReasons(_publishedVariationReasons, _channel);

            result
                .Should()
                .ThrowExactly<KeyNotFoundException>();

            VariationReasonsAreNotPersisted();
        }

        [TestMethod]
        public void WhenVariationReasonsNotSet_ThrowsNullReferenceException()
        {
            GivenVariationReasons(null);
            GivenContext(_providerVersionChannels);

            Func<Task> result = async () => await _service.PopulateReleasedProviderChannelVariationReasons(_publishedVariationReasons, _channel);

            result
                .Should()
                .ThrowExactly<NullReferenceException>();

            VariationReasonsAreNotPersisted();
        }

        private void VariationReasonsArePersisted()
        {
            _releaseManagementRepository.Verify(_ =>
                            _.CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(It.IsAny<IEnumerable<ReleasedProviderChannelVariationReason>>()), Times.Exactly(_publishedVariationReasons.Count));
        }

        private void VariationReasonsAreNotPersisted()
        {
            _releaseManagementRepository.Verify(_ =>
                            _.CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(It.IsAny<IEnumerable<ReleasedProviderChannelVariationReason>>()), Times.Never);
        }

        private void GivenVariationReasons(IEnumerable<Publishing.FundingManagement.SqlModels.VariationReason> variationReasons)
        {
            _releaseManagementRepository.Setup(s => s.GetVariationReasons())
                .ReturnsAsync(variationReasons);
        }

        private void GivenContext(Dictionary<string, ReleasedProviderVersionChannel> providerVersionChannels)
        {
            if (providerVersionChannels == null)
            {
                providerVersionChannels = new Dictionary<string, ReleasedProviderVersionChannel>();
            }

            _releaseToChannelSqlMappingContext.SetupGet(s => s.ReleasedProviderVersionChannels)
                .Returns(providerVersionChannels);
        }
    }
}
