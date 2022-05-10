using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class PublishedProviderStateSummaryCsvTransform : IPublishedProviderStateSummaryCsvTransform
    {
        private readonly ArrayPool<ExpandoObject> _expandoObjectsPool
            = ArrayPool<ExpandoObject>.Create(PublishedProviderStateSummaryCsvGenerator.BatchSize, 4);

        public bool IsForJobDefinition(string jobDefinitionName)
        {
            return jobDefinitionName == JobConstants.DefinitionNames.GeneratePublishedProviderStateSummaryCsvJob;
        }

        public IEnumerable<ExpandoObject> Transform(FundingConfiguration fundingConfiguration,
                                                    IDictionary<string, PublishedProvider> publishedProviders,
                                                        IDictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId)
        {
            //PublishedSearchResult
            int resultsCount = publishedProviders.Count;
            ExpandoObject[] resultsBatch = _expandoObjectsPool.Rent(resultsCount);

            IEnumerable<string> distinctChannels = fundingConfiguration?.ReleaseChannels?.Where(_ => _.IsVisible).Select(_ => _.ChannelCode)?.Distinct()?.OrderBy(_ => _).ToList()
                                                    ?? new List<string>();
            
            for (int resultCount = 0; resultCount < resultsCount; resultCount++)
            {
                PublishedProviderVersion publishedProviderVersion = GetPublishedProviderVersion(publishedProviders, resultCount);

                IDictionary<string, object> row = resultsBatch[resultCount] ?? (resultsBatch[resultCount] = new ExpandoObject());

                Provider provider = publishedProviderVersion.Provider;
                IEnumerable<ReleaseChannel> providerChannels;
                releaseChannelLookupByProviderId.TryGetValue(provider.ProviderId, out providerChannels);

                row["UKPRN"] = provider.UKPRN;
                row["URN"] = provider.URN;
                row["Establishment Number"] = provider.EstablishmentNumber;
                row["Provider Name"] = provider.Name;
                row["Provider Type"] = provider.ProviderType;
                row["Provider SubType"] = provider.ProviderSubType;
                row["LA Code"] = provider.LACode;
                row["LA Name"] = provider.Authority;
                row["Allocation Status"] = publishedProviderVersion.Status.ToString();
                row["Allocation Major Version"] = publishedProviderVersion.MajorVersion.ToString();
                row["Allocation Minor Version"] = publishedProviderVersion.MinorVersion.ToString();
                row["Allocation Author"] = publishedProviderVersion.Author?.Name;
                row["Allocation DateTime"] = publishedProviderVersion.Date.ToString("s");
                row["Is Indicative"] = publishedProviderVersion.IsIndicative.ToString();

                List<int> releasedMajorVersions = new List<int>();
                foreach (string channel in distinctChannels)
                {
                    ReleaseChannel providerChannel = providerChannels?.Where(_ => _.ChannelCode == channel)?.FirstOrDefault();
                    row[$"{channel} released version"] = providerChannel?.MajorVersion;
                    releasedMajorVersions.Add(providerChannel?.MajorVersion ?? 0);
                }

                row["Release candidate"] = releasedMajorVersions.Any(_ => _ < publishedProviderVersion.MajorVersion);

                yield return (ExpandoObject)row;
            }

            _expandoObjectsPool.Return(resultsBatch);
        }

        protected PublishedProviderVersion GetPublishedProviderVersion(IDictionary<string, PublishedProvider> publishedProviders, int resultCount)
        {
            return publishedProviders.ElementAt(resultCount).Value.Current;
        }
    }
}
