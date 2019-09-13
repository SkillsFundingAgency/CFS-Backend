using System;
using System.Collections.Concurrent;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingIdGeneratorResolver : IPublishedFundingIdGeneratorResolver
    {
        private readonly ConcurrentDictionary<string, IPublishedFundingIdGenerator> _supportedVersions;

        public PublishedFundingIdGeneratorResolver()
        {
            _supportedVersions = new ConcurrentDictionary<string, IPublishedFundingIdGenerator>();
        }

        public bool Contains(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            return _supportedVersions.ContainsKey(schemaVersion);
        }

        /// <summary>
        /// Get a resolver registered to the schema version
        /// </summary>
        /// <param name="schemaVersion">The schema version</param>
        /// <returns>A resolver regsitered for the schema value</returns>
        /// <exception cref="Exception">Thrown when no resolver registered for schema value</exception>
        public IPublishedFundingIdGenerator GetService(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            IPublishedFundingIdGenerator publishedFundingIdGenerator;

            if (_supportedVersions.TryGetValue(schemaVersion, out publishedFundingIdGenerator))
            {
                return publishedFundingIdGenerator;
            }
            else
            {
                throw new Exception($"Unable to find a registered resolver for schema version : {schemaVersion}");
            }
        }

        public void Register(string schemaVersion, IPublishedFundingIdGenerator publishedFundingIdGenerator)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));
            Guard.ArgumentNotNull(publishedFundingIdGenerator, nameof(publishedFundingIdGenerator));

            _supportedVersions.TryAdd(schemaVersion, publishedFundingIdGenerator);
        }

        public bool TryGetService(string schemaVersion, out IPublishedFundingIdGenerator publishedFundingIdGenerator)
        {
            return _supportedVersions.TryGetValue(schemaVersion, out publishedFundingIdGenerator);
        }
    }
}
