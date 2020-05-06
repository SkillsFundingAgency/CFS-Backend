using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedFundingVersionInMemoryRepository : IVersionRepository<PublishedFundingVersion>
    {
        private readonly ReaderWriterLockSlim lockVersionObject = new ReaderWriterLockSlim();

        ConcurrentDictionary<string, PublishedFundingVersion> _publishedFundingVersions = new ConcurrentDictionary<string, PublishedFundingVersion>();

        VersionRepository<PublishedFundingVersion> _realImplementation = new VersionRepository<PublishedFundingVersion>(new InMemoryCosmosRepository());

        public Task<PublishedFundingVersion> CreateVersion(PublishedFundingVersion newVersion, PublishedFundingVersion currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
        {
            return _realImplementation.CreateVersion(newVersion, currentVersion, partitionKey, incrementFromCurrentVersion);
        }

        public Task DeleteVersions(IEnumerable<KeyValuePair<string, PublishedFundingVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNextVersionNumber(PublishedFundingVersion version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedFundingVersion> GetVersion(string entityId, int version)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedFundingVersion>> GetVersions(string entityId, string partitionKeyId = null)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SaveVersion(PublishedFundingVersion newVersion)
        {
            if (_publishedFundingVersions.ContainsKey(newVersion.Id))
            {
                throw new InvalidOperationException("Unable to save version, existing one already exists");
            }

            _publishedFundingVersions[newVersion.Id] = newVersion;

            return Task.FromResult(HttpStatusCode.OK);
        }

        public Task SaveVersion(PublishedFundingVersion newVersion, string partitionKey)
        {
            if (_publishedFundingVersions.ContainsKey(newVersion.Id))
            {
                throw new InvalidOperationException("Unable to save version, existing one already exists");
            }

            _publishedFundingVersions[newVersion.Id] = newVersion;

            return Task.FromResult(newVersion);
        }

        public Task SaveVersions(IEnumerable<PublishedFundingVersion> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task SaveVersions(IEnumerable<KeyValuePair<string, PublishedFundingVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }
    }
}
