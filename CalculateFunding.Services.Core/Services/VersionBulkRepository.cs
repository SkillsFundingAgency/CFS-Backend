using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;

namespace CalculateFunding.Services.Core.Services
{
    public class VersionBulkRepository<T> : IVersionBulkRepository<T> where T : VersionedItem
    {
        protected readonly ICosmosRepository CosmosRepository;

        private readonly INewVersionBuilderFactory<T> _newVersionBuilderFactory;

        public VersionBulkRepository(ICosmosRepository cosmosRepository,
            INewVersionBuilderFactory<T> newVersionBuilderFactory)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(newVersionBuilderFactory, nameof(newVersionBuilderFactory));

            CosmosRepository = cosmosRepository;
            _newVersionBuilderFactory = newVersionBuilderFactory;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool ok, string message) = CosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(VersionBulkRepository<T>)
            };
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = ok,
                DependencyName = CosmosRepository.GetType().GetFriendlyName(),
                Message = message
            });

            return Task.FromResult(health);
        }

        public async Task<T> SaveVersion(T newVersion,
            string partitionKey)
        {
            await CosmosRepository.CreateAsync(newVersion, partitionKey);

            return newVersion;
        }

        public async Task<HttpStatusCode> SaveVersion(T newVersion)
          => await CosmosRepository.CreateAsync(newVersion);

        public async Task<T> CreateVersion(T newVersion,
            T currentVersion = null,
            string partitionKey = null,
            bool incrementFromCurrentVersion = false)
        {
            ICreateVersions<T> newVersionBuilder = _newVersionBuilderFactory.CreateNewVersionBuild(CosmosRepository);

            return await newVersionBuilder.CreateVersion(newVersion,
                currentVersion,
                partitionKey,
                incrementFromCurrentVersion);
        }
    }

    public interface INewVersionBuilderFactory<T> where T : VersionedItem
    {
        ICreateVersions<T> CreateNewVersionBuild(ICosmosRepository cosmosRepository);
    }

    public class NewVersionBuilder<T> : ICreateVersions<T>
        where T : VersionedItem
    {
        private readonly ICosmosRepository _cosmosRepository;

        public NewVersionBuilder(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public async Task<T> CreateVersion(T newVersion,
            T currentVersion = default,
            string partitionKey = null,
            bool incrementFromCurrentVersion = false)
        {
            Guard.ArgumentNotNull(newVersion, nameof(newVersion));

            newVersion.Date = DateTimeOffset.Now.ToLocalTime();

            if (currentVersion == null)
            {
                newVersion.Version = 1;

                newVersion.PublishStatus = PublishStatus.Draft;
            }
            else
            {
                newVersion.Version = await GetNextVersionNumber(newVersion, currentVersion.Version, partitionKey, incrementFromCurrentVersion);

                if (newVersion.PublishStatus == PublishStatus.Approved && (currentVersion.PublishStatus == PublishStatus.Draft || currentVersion.PublishStatus == PublishStatus.Updated))
                {
                    return newVersion;
                }

                switch (currentVersion.PublishStatus)
                {
                    case PublishStatus.Draft:
                        newVersion.PublishStatus = PublishStatus.Draft;
                        break;

                    case PublishStatus.Approved:
                        if (newVersion.PublishStatus != PublishStatus.Draft)
                        {
                            newVersion.PublishStatus = PublishStatus.Updated;
                        }

                        break;

                    default:
                        newVersion.PublishStatus = PublishStatus.Updated;
                        break;
                }
            }

            return newVersion;
        }

        public async Task<int> GetNextVersionNumber(T version = null,
            int currentVersion = 0,
            string partitionKeyId = null,
            bool incrementFromCurrentVersion = false)
        {
            Guard.ArgumentNotNull(version, nameof(version));

            if (incrementFromCurrentVersion)
            {
                return currentVersion + 1;
            }

            CosmosDbQuery cosmosDbQuery;
            if (string.IsNullOrWhiteSpace(partitionKeyId))
            {
                string entityId = version.EntityId;

                cosmosDbQuery = new CosmosDbQuery
                {
                    QueryText = @"SELECT VALUE Max(c.content.version) 
                            FROM    c 
                            WHERE   c.content.entityId = @EntityID
                                    AND c.documentType = @DocumentType",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@EntityID", entityId),
                        new CosmosDbQueryParameter("@DocumentType", typeof(T).Name)
                    }
                };
            }
            else
            {
                cosmosDbQuery = new CosmosDbQuery
                {
                    QueryText = @"SELECT VALUE Max(c.content.version) 
                            FROM    c 
                            WHERE   c.documentType = @DocumentType",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@DocumentType", typeof(T).Name)
                    }
                };
            }

            IEnumerable<dynamic> results;

            if (string.IsNullOrWhiteSpace(partitionKeyId))
            {
                results = await _cosmosRepository.DynamicQuery(cosmosDbQuery);
            }
            else
            {
                results = await _cosmosRepository.DynamicQueryPartitionedEntity<dynamic>(cosmosDbQuery, partitionKeyId);
            }

            if (results.IsNullOrEmpty()) return 1;

            int nextVersionNumber = (int)results.First() + 1;

            return nextVersionNumber;
        }
    }

    public class NewVersionBuilderFactory<T> : INewVersionBuilderFactory<T> where T : VersionedItem
    {
        public ICreateVersions<T> CreateNewVersionBuild(ICosmosRepository cosmosRepository) => new NewVersionBuilder<T>(cosmosRepository);
    }

    public interface ICreateVersions<T>
        where T : VersionedItem
    {
        Task<T> CreateVersion(T newVersion,
            T currentVersion = null,
            string partitionKey = null,
            bool incrementFromCurrentVersion = false);

        Task<int> GetNextVersionNumber(T version = null,
            int currentVersion = 0,
            string partitionKeyId = null,
            bool incrementFromCurrentVersion = false);
    }
}