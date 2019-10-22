using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedProviderVersionInMemoryRepository : IVersionRepository<PublishedProviderVersion>
    {
        Dictionary<string, PublishedProviderVersion> _publishedProviderVersions = new Dictionary<string, PublishedProviderVersion>();

        VersionRepository<PublishedProviderVersion> _realImplementation = new VersionRepository<PublishedProviderVersion>(new InMemoryCosmosRepository());

        public Task<PublishedProviderVersion> CreateVersion(PublishedProviderVersion newVersion, PublishedProviderVersion currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
        {
            return _realImplementation.CreateVersion(newVersion, currentVersion, partitionKey, incrementFromCurrentVersion);
        }

        public Task<int> GetNextVersionNumber(PublishedProviderVersion version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderVersion> GetVersion(string entityId, int version)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetVersions(string entityId, string partitionKeyId = null)
        {
            throw new NotImplementedException();
        }

        public Task SaveVersion(PublishedProviderVersion newVersion)
        {
            if (_publishedProviderVersions.ContainsKey(newVersion.Id))
            {
                throw new InvalidOperationException("Unable to save version, existing one already exists");
            }         

            _publishedProviderVersions[newVersion.Id] = newVersion;

            return Task.FromResult(newVersion);
        }

        public Task SaveVersion(PublishedProviderVersion newVersion, string partitionKey)
        {
            if (_publishedProviderVersions.ContainsKey(newVersion.Id))
            {
                throw new InvalidOperationException("Unable to save version, existing one already exists");
            }

            _publishedProviderVersions[newVersion.Id] = newVersion;

            return Task.FromResult(newVersion);
        }

        public Task SaveVersions(IEnumerable<PublishedProviderVersion> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task SaveVersions(IEnumerable<KeyValuePair<string, PublishedProviderVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            if (newVersions.AnyWithNullCheck())
            {
                foreach (var version in newVersions)
                {
                    _publishedProviderVersions[version.Key] = version.Value;
                }
            }

            return Task.CompletedTask;
        }
    }
}
