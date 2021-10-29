using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using System;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedProviderLoaderForFundingGroupDataTests
    {
        private Mock<IReleaseManagementRepository> _repo;
        private Mock<IPublishedProvidersLoadContext> _context;
        private PublishedProviderLoaderForFundingGroupData _service;
        private readonly string _specificationId = new RandomString();
        private readonly int _channelId = new RandomNumberBetween(1, 10);
        private readonly string providerIdInBatch = new RandomString();
        private readonly string providerIdNotInBatch = new RandomString();
        private readonly int majorVersion = new RandomNumberBetween(1, 10);
        private List<string> _batchPublishedProviderIds;
        private List<OrganisationGroupResult> _organisationGroupResults;

        [TestInitialize]
        public void Initialize()
        {
            _organisationGroupResults = new List<OrganisationGroupResult>
            {
                new OrganisationGroupResult
                {
                    Providers = new List<Provider>
                    {
                        new Provider { ProviderId = providerIdInBatch },
                        new Provider { ProviderId = providerIdNotInBatch }
                    }
                }
            };
            _batchPublishedProviderIds = new List<string> { providerIdInBatch };
            _repo = new Mock<IReleaseManagementRepository>();
            _context = new Mock<IPublishedProvidersLoadContext>();
            _context.Setup(_ => _.GetOrLoadProvider(It.Is<string>(_ => _ == providerIdNotInBatch), It.Is<int>(_ => _ == majorVersion)))
                .ReturnsAsync(new PublishedProvider { Current = new PublishedProviderVersion { ProviderId = providerIdNotInBatch } });
            _context.Setup(_ => _.GetOrLoadProviders(It.Is<IEnumerable<string>>(s => s.SequenceEqual(_batchPublishedProviderIds))))
                .ReturnsAsync(new List<PublishedProvider>
                {
                    new PublishedProvider { Current = new PublishedProviderVersion { ProviderId = providerIdInBatch } }
                });
            _service = new PublishedProviderLoaderForFundingGroupData(_context.Object, _repo.Object);
        }

        [TestMethod]
        public async Task GetsAllProviders_Successfully()
        {
            GivenExistingProviders();

            List<PublishedProvider> result = await _service.GetAllPublishedProviders(_organisationGroupResults, _specificationId, _channelId, _batchPublishedProviderIds);

            result
                .Should()
                .HaveCount(_organisationGroupResults.SelectMany(s => s.Providers).Count());

            result
                .Select(s => s.Current.ProviderId)
                .Should()
                .Contain(new List<string> { providerIdInBatch, providerIdNotInBatch });

            _context.Verify(_ => _.GetOrLoadProvider(It.Is<string>(s => s == providerIdNotInBatch), It.Is<int>(i => i == majorVersion)), Times.Once);
            _context.Verify(_ => _.GetOrLoadProviders(It.Is<IEnumerable<string>>(p => p.Count() == _batchPublishedProviderIds.Count())), Times.Once);
        }

        [TestMethod]
        public void WhenProviderVersionNotFound_Throws()
        {
            GivenMissingProviders();

            Func<Task> result = () => _service.GetAllPublishedProviders(_organisationGroupResults, _specificationId, _channelId, _batchPublishedProviderIds);

            result
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage($"Provider version not found for providerId {providerIdNotInBatch}");

            _context.Verify(_ => _.GetOrLoadProvider(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _context.Verify(_ => _.GetOrLoadProviders(It.Is<IEnumerable<string>>(p => p.Count() == _batchPublishedProviderIds.Count())), Times.Once);
        }

        private void GivenExistingProviders()
        {
            _repo.Setup(_ => _.GetLatestPublishedProviderVersions(It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<ProviderVersionInChannel>
                {
                    new ProviderVersionInChannel
                    {
                        ProviderId = providerIdNotInBatch,
                        MajorVersion = majorVersion
                    }
                });
        }

        private void GivenMissingProviders()
        {
            _repo.Setup(_ => _.GetLatestPublishedProviderVersions(It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<ProviderVersionInChannel>
                {
                    new ProviderVersionInChannel
                    {
                        ProviderId = new RandomString(),
                        MajorVersion = majorVersion
                    }
                });
        }
    }
}
