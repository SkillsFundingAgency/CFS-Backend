using System;
using System.Collections.Concurrent;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderContentsGeneratorResolver : IPublishedProviderContentsGeneratorResolver
    {
        private readonly ConcurrentDictionary<string, IPublishedProviderContentsGenerator> _supportedVersions;

        public PublishedProviderContentsGeneratorResolver()
        {
            _supportedVersions = new ConcurrentDictionary<string, IPublishedProviderContentsGenerator>();
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
        public IPublishedProviderContentsGenerator GetService(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            IPublishedProviderContentsGenerator templateMetadataGenerator;

            if (_supportedVersions.TryGetValue(schemaVersion, out templateMetadataGenerator))
            {
                return templateMetadataGenerator;
            }
            else
            {
                throw new Exception($"Unable to find a registered resolver for schema version : {schemaVersion}");
            }
        }

        public void Register(string schemaVersion, IPublishedProviderContentsGenerator publishedProviderContentsGenerator)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));
            Guard.ArgumentNotNull(publishedProviderContentsGenerator, nameof(publishedProviderContentsGenerator));

            _supportedVersions.TryAdd(schemaVersion, publishedProviderContentsGenerator);
        }

        public bool TryGetService(string schemaVersion, out IPublishedProviderContentsGenerator publishedProviderContentsGenerator)
        {
            return _supportedVersions.TryGetValue(schemaVersion, out publishedProviderContentsGenerator);
        }
    }
}
