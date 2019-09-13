using System;
using System.Collections.Concurrent;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingContentsGeneratorResolver : IPublishedFundingContentsGeneratorResolver
    {
        private readonly ConcurrentDictionary<string, IPublishedFundingContentsGenerator> _supportedVersions;

        public PublishedFundingContentsGeneratorResolver()
        {
            _supportedVersions = new ConcurrentDictionary<string, IPublishedFundingContentsGenerator>();
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
        public IPublishedFundingContentsGenerator GetService(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            IPublishedFundingContentsGenerator templateMetadataGenerator;

            if (_supportedVersions.TryGetValue(schemaVersion, out templateMetadataGenerator))
            {
                return templateMetadataGenerator;
            }
            else
            {
                throw new Exception($"Unable to find a registered resolver for schema version : {schemaVersion}");
            }
        }

        public void Register(string schemaVersion, IPublishedFundingContentsGenerator publishedFundingContentsGenerator)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));
            Guard.ArgumentNotNull(publishedFundingContentsGenerator, nameof(publishedFundingContentsGenerator));

            _supportedVersions.TryAdd(schemaVersion, publishedFundingContentsGenerator);
        }

        public bool TryGetService(string schemaVersion, out IPublishedFundingContentsGenerator publishedFundingContentsGenerator)
        {
            return _supportedVersions.TryGetValue(schemaVersion, out publishedFundingContentsGenerator);
        }
    }
}
