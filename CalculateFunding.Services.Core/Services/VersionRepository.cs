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
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Core.Services
{
    public class VersionRepository<T> : IVersionRepository<T> where T : VersionedItem
    {
        protected readonly ICosmosRepository CosmosRepository;

        private readonly INewVersionBuilderFactory<T> _newVersionBuilderFactory;

        public VersionRepository(ICosmosRepository cosmosRepository,
            INewVersionBuilderFactory<T> newVersionBuilderFactory)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(newVersionBuilderFactory, nameof(newVersionBuilderFactory));

            CosmosRepository = cosmosRepository;
            _newVersionBuilderFactory = newVersionBuilderFactory;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = CosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(VersionRepository<T>)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = CosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public async Task<HttpStatusCode> SaveVersion(T newVersion)
        {
            Guard.ArgumentNotNull(newVersion, nameof(newVersion));
            return await CosmosRepository.CreateAsync<T>(newVersion);
        }

        public async Task SaveVersion(T newVersion, string partitionKey)
        {
            Guard.ArgumentNotNull(newVersion, nameof(newVersion));
            Guard.IsNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            await CosmosRepository.CreateAsync<T>(newVersion, partitionKey);
        }

        public async Task SaveVersions(IEnumerable<T> newVersions, int maxDegreesOfParallelism = 30)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            await CosmosRepository.BulkCreateAsync<T>(newVersions.ToList(), degreeOfParallelism: maxDegreesOfParallelism);
        }

        public async Task SaveVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            await CosmosRepository.BulkCreateAsync<T>(newVersions.ToList(), degreeOfParallelism: maxDegreesOfParallelism);
        }

        public async Task DeleteVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            await CosmosRepository.BulkDeleteAsync(newVersions.ToList(), degreeOfParallelism: maxDegreesOfParallelism);
        }

        public async Task<T> CreateVersion(T newVersion, T currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
        {
            ICreateVersions<T> newVersionBuilder = _newVersionBuilderFactory.CreateNewVersionBuild(CosmosRepository);

            return await newVersionBuilder.CreateVersion(newVersion,
                currentVersion,
                partitionKey,
                incrementFromCurrentVersion);
        }


        public async Task<T> GetVersion(string entityId, int version)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            if (version < 1)
            {
                throw new ArgumentException("Invalid version number was supplied", nameof(version));
            }

            IEnumerable<T> versions = await CosmosRepository.Query<T>(m => m.Content.EntityId == entityId && m.Content.Version == version);

            return versions.FirstOrDefault();
        }

        public async Task<int> GetNextVersionNumber(T version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            ICreateVersions<T> newVersionBuilder = _newVersionBuilderFactory.CreateNewVersionBuild(CosmosRepository);

            return await newVersionBuilder.GetNextVersionNumber(version,
                currentVersion,
                partitionKeyId,
                incrementFromCurrentVersion);
        }

        public async Task<IEnumerable<T>> GetVersions(string entityId, string partitionKeyId = null)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            if (partitionKeyId == null)
            {
                return await CosmosRepository.Query<T>(m => m.Content.EntityId == entityId);
            }
            else
            {
                CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                {
                    QueryText = "SELECT * FROM Root c WHERE c.documentType = @DocumentType AND c.deleted = false AND c.content.entityId = @EntityID",
                    Parameters = new[]
                    {
                        new CosmosDbQueryParameter("@DocumentType", typeof(T).Name),
                        new CosmosDbQueryParameter("@EntityID", entityId)
                    }
                };

                return await CosmosRepository.QueryPartitionedEntity<T>(cosmosDbQuery, partitionKey: partitionKeyId);
            }
        }
    }
}