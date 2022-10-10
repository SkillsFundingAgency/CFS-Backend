using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ReleaseProviderPersistenceServiceTests
    {
        private Mock<IUniqueIdentifierProvider> _identifierGenerator;
        private ReleaseProviderPersistenceService _service;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private IEnumerable<string> _providers;
        private string _specificationId;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _releaseToChannelSqlMappingContext.SetupGet(_ => _.ReleasedProviders).Returns(new Dictionary<string, ReleasedProvider>());
            _identifierGenerator = new Mock<IUniqueIdentifierProvider>();
            _logger = new Mock<ILogger>();
            _service = new ReleaseProviderPersistenceService(_releaseToChannelSqlMappingContext.Object,
                _releaseManagementRepository.Object,
                _identifierGenerator.Object,
                 _logger.Object
                );
        }

        [TestMethod]
        public async Task WhenGivenProvidersNotInContextThenTheyAreSuccessfullyReleased()
        {
            string provider = new RandomString();
            string providerReleased = new RandomString();

            GivenProviders(provider, providerReleased);
            GivenReleasedProvidersInContext(providerReleased);
            await WhenProvidersReleased();
            ThenProvidersAreReleased(provider);
            AndTheContextIsUpdated(provider);
        }

        private async Task WhenProvidersReleased()
        {
            await _service.ReleaseProviders(_providers, _specificationId);
        }

        private void GivenProviders(params string[] providers)
        {
            _providers = providers;
        }

        private void GivenReleasedProvidersInContext(params string[] providersInContext)
        {
            _releaseToChannelSqlMappingContext.SetupGet(_ => _.ReleasedProviders)
                .Returns(_providers.Where(_ =>
                        providersInContext.Any(pic => pic == _)).Select(_ =>
                            new ReleasedProvider { ProviderId = _ }).ToDictionary(_ => _.ProviderId));
        }

        private void ThenProvidersAreReleased(params string[] providers)
        {
            _releaseManagementRepository.Verify(_ =>
                _.BulkCreateReleasedProvidersUsingAmbientTransaction(It.Is<IEnumerable<ReleasedProvider>>(
                    rps => rps.Select(s => s.ProviderId).SequenceEqual(providers))), Times.Once());
        }

        private void AndTheContextIsUpdated(params string[] providers)
        {
            _releaseToChannelSqlMappingContext.Object.ReleasedProviders.Select(_ => _.Key).SequenceEqual(providers);
        }
    }
}
