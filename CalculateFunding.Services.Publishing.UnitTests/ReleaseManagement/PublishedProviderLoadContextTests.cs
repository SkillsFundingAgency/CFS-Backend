using AutoFixture;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedProviderLoadContextTests
    {
        private const int TotalProviderCount = 3;
        private const string FundingStreamId = "DSG";
        private const string FundingPeriodId = "FY2021";
        private Mock<IPublishedFundingBulkRepository> _bulkRepo;
        private Mock<IPublishedFundingRepository> _repo;
        private PublishedProvidersLoadContext _sut;
        private Fixture _fixture;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private PublishedProvider _publishedProvider;
        private IEnumerable<string> _providerIds;

        [TestInitialize]
        public void Initialise()
        {
            _bulkRepo = new Mock<IPublishedFundingBulkRepository>();
            _repo = new Mock<IPublishedFundingRepository>();
            _sut = new PublishedProvidersLoadContext(_bulkRepo.Object, _repo.Object);
            _fixture = new Fixture();
            _publishedProviders = _fixture.CreateMany<PublishedProvider>(TotalProviderCount);
            _publishedProvider = _publishedProviders.First();
            _providerIds = _publishedProviders.Select(s => s.Current.ProviderId);
        }

        [TestMethod]
        public void CanAddProvidersToContext()
        {
            _sut.AddProviders(_publishedProviders);

            _sut.Count
                .Should()
                .Be(TotalProviderCount);

            foreach (PublishedProvider provider in _publishedProviders)
            {
                _sut.ContainsKey(provider.Current.ProviderId)
                    .Should()
                    .BeTrue();

                _sut[provider.Current.ProviderId]
                    .Should()
                    .BeEquivalentTo(provider);
            }
        }

        [TestMethod]
        public void AttemptingToLoadProvidersWithoutSettingFundingStream_ThrowsInvalidOperationException()
        {
            Func<Task> action = async () => await _sut.LoadProviders(_providerIds);

            action
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Funding stream not set");
        }

        [TestMethod]
        public async Task LoadProvidersSetsProviders()
        {
            GivenBulkRepoReturnsPublishedProviders();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            await _sut.LoadProviders(_providerIds);

            _sut.Count
                .Should()
                .Be(TotalProviderCount);
        }

        [TestMethod]
        public async Task LoadProviderSetsAndReturnsProvider()
        {
            GivenRepoReturnsProvider();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            PublishedProvider result = await _sut.LoadProvider(_publishedProvider.Current.ProviderId);

            _sut.Count
                .Should()
                .Be(1);

            result
                .Should()
                .BeEquivalentTo(_publishedProvider);
        }

        [TestMethod]
        public async Task LoadProviderVersionSetsAndReturnsProviderVersion()
        {
            GivenRepoReturnsProviderVersion();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            PublishedProviderVersion result = await _sut.LoadProviderVersion(_publishedProvider.Released.ProviderId, _publishedProvider.Released.MajorVersion);

            result
                .Should()
                .BeEquivalentTo(_publishedProvider.Released);
        }

        [TestMethod]
        public void LoadProvider_ForMissingProvider_ThrowsInvalidOperationException()
        {
            GivenRepoReturnsNull();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            Func<Task> result = async () => await _sut.LoadProvider("123");

            result
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Published provider with provider ID 123 not found");
        }

        [TestMethod]
        public void LoadProviderVersion_ForMissingProvider_ThrowsInvalidOperationException()
        {
            GivenRepoReturnsNull();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            Func<Task> result = async () => await _sut.LoadProviderVersion("123", 1);

            result
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Published provider with provider ID 123 and major version 1 not found");
        }

        [TestMethod]
        public async Task GetOrLoadProvidersReturnsProviders_WhereTheyAlreadyExistInMemory()
        {
            GivenPublishedProvidersExistInMemory();

            IEnumerable<PublishedProvider> result = await _sut.GetOrLoadProviders(_providerIds);

            result
                .Should()
                .BeEquivalentTo(_publishedProviders);

            AssertProvidersNotRetrievedFromRepo();
        }

        [TestMethod]
        public async Task GetOrLoadProvidersReturnsProviders_WhereTheyDoNotExistInMemory()
        {
            GivenBulkRepoReturnsPublishedProviders();

            _sut.SetSpecDetails(FundingStreamId, FundingPeriodId);

            IEnumerable<PublishedProvider> result = await _sut.GetOrLoadProviders(_publishedProviders.Select(s => s.Current.ProviderId));

            result
                .Should()
                .BeEquivalentTo(_publishedProviders);

            AssertProvidersWereRetrievedFromRepo();
        }

        private void GivenRepoReturnsNull()
        {
            _repo.Setup(s => s.GetPublishedProvider(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .ReturnsAsync((PublishedProvider)null);
        }

        private void GivenRepoReturnsProvider()
        {
            _repo.Setup(s => s.GetPublishedProvider(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                            .ReturnsAsync(_publishedProvider);
        }

        private void GivenRepoReturnsProviderVersion()
        {
            _repo.Setup(s => s.GetReleasedPublishedProviderVersion(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                            .ReturnsAsync(_publishedProvider.Released);
        }

        private void GivenBulkRepoReturnsPublishedProviders()
        {
            _bulkRepo.Setup(s => s.TryGetPublishedProvidersByProviderId(
                            It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>()))
                            .ReturnsAsync(_publishedProviders);
        }

        private void GivenPublishedProvidersExistInMemory()
        {
            _sut.AddProviders(_publishedProviders);
        }

        private void AssertProvidersNotRetrievedFromRepo()
        {
            _bulkRepo.Verify(s => s.TryGetPublishedProvidersByProviderId(
                            It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private void AssertProvidersWereRetrievedFromRepo()
        {
            _bulkRepo.Verify(s => s.TryGetPublishedProvidersByProviderId(
                            It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
