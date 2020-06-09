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
        ///     Get a resolver registered to the schema version
        /// </summary>
        /// <param name="schemaVersion">The schema version</param>
        /// <returns>A resolver registered for the schema value</returns>
        /// <exception cref="Exception">Thrown when no resolver registered for schema value</exception>
        public IPublishedFundingContentsGenerator GetService(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            return TryGetService(schemaVersion, out IPublishedFundingContentsGenerator templateMetadataGenerator)
                ? templateMetadataGenerator
                : throw new ArgumentOutOfRangeException(nameof(schemaVersion), 
                    $"Unable to find a registered resolver for schema version : {schemaVersion}");
        }

        public void Register(string schemaVersion,
            IPublishedFundingContentsGenerator publishedFundingContentsGenerator)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));
            Guard.ArgumentNotNull(publishedFundingContentsGenerator, nameof(publishedFundingContentsGenerator));

            _supportedVersions.TryAdd(schemaVersion, publishedFundingContentsGenerator);
        }

        public bool TryGetService(string schemaVersion,
            out IPublishedFundingContentsGenerator publishedFundingContentsGenerator) 
            => _supportedVersions.TryGetValue(schemaVersion, out publishedFundingContentsGenerator);
    }
}