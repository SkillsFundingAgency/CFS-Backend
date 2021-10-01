using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
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
    public class ReleaseProviderPersistenceServiceTests
    {
        private ReleaseProviderPersistanceService _service;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private IEnumerable<string> _providers;
        private string _specificationId;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _releaseToChannelSqlMappingContext.SetupGet(_ => _.ReleasedProviders).Returns(new Dictionary<string, ReleasedProvider>());
            _service = new ReleaseProviderPersistanceService(_releaseToChannelSqlMappingContext.Object,
                _releaseManagementRepository.Object);
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
                _.CreateReleasedProvidersUsingAmbientTransaction(It.Is<IEnumerable<ReleasedProvider>>(_ => 
                    _.Select(rp => rp.ProviderId).SequenceEqual(providers))), Times.Once);
        }

        private void AndTheContextIsUpdated(params string[] providers)
        {
            _releaseToChannelSqlMappingContext.Object.ReleasedProviders.Select(_ => _.Key).SequenceEqual(providers);
        }
    }
}
