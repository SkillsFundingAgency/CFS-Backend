﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Providers.ViewModels;
using CalculateFunding.Common.Models.Search;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class ProvidersInMemoryClient : IProvidersApiClient
    {
        private Dictionary<string, Dictionary<string, Provider>> _providers = new Dictionary<string, Dictionary<string, Provider>>();
        private Dictionary<string, Dictionary<string, Provider>> _scopedProviders = new Dictionary<string, Dictionary<string, Provider>>();
        private Dictionary<string, ProviderVersion> _providerVersions = new Dictionary<string, ProviderVersion>();

        public Task<HttpStatusCode> DoesProviderVersionExist(string providerVersionId)
        {
            throw new NotImplementedException();
        }


        public Task<ApiResponse<IEnumerable<ProviderSummary>>> FetchCoreProviderData(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersion>> GetAllMasterProviders()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResult>> GetProviderByIdFromMaster(string providerId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResult>> GetProviderByIdFromProviderVersion(string providerVersionId, string providerId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersion>> GetProvidersByVersion(int year, int month, int day)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersion>> GetProvidersByVersion(string providerVersionId)
        {
            ApiResponse<ProviderVersion> result;
            if (_providerVersions.ContainsKey(providerVersionId))
            {
                result = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, _providerVersions[providerVersionId]);
            }
            else
            {
                result = new ApiResponse<ProviderVersion>(HttpStatusCode.NotFound);
            }


            return Task.FromResult(result);
        }

        public Task<ApiResponse<ProviderVersionMetadata>> GetProviderVersionMetadata(string providerVersionId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<ProviderVersionMetadata>>> GetProviderVersionsByFundingStream(string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<string>>> GetScopedProviderIds(string specificationId)
        {
            ApiResponse<IEnumerable<string>> result;
            if (_scopedProviders.ContainsKey(specificationId))
            {
                result = new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, _scopedProviders[specificationId].Values.Select(c => c.ProviderId));
            }
            else
            {
                result = new ApiResponse<IEnumerable<string>>(HttpStatusCode.NotFound);
            }

            return Task.FromResult(result);
        }

        public Task<ApiResponse<int?>> PopulateProviderSummariesForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<ProviderVersionSearchResult>> SearchMasterProviders(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResults>> SearchMasterProviders(SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResults>> SearchProvidersInProviderVersion(string providerVersionId, SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResults>> SearchProviderVersions(int year, int month, int day, SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<ProviderVersionSearchResults>> SearchProviderVersions(SearchModel searchModel)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SetMasterProviderVersion(MasterProviderVersionViewModel masterProviderVersion)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SetProviderDateProviderVersion(int year, int month, int day, string providerVersionId)
        {
            throw new NotImplementedException();
        }

        public Task<NoValidatedContentApiResponse> UploadProviderVersion(string providerVersionId, ProviderVersionViewModel providers)
        {
            throw new NotImplementedException();
        }

        public void AddProviderVersion(ProviderVersion providerVersion)
        {
            Guard.ArgumentNotNull(providerVersion, nameof(providerVersion));
            Guard.IsNullOrWhiteSpace(providerVersion.ProviderVersionId, nameof(providerVersion.ProviderVersionId));

            _providerVersions[providerVersion.ProviderVersionId] = providerVersion;
            if (!_providers.ContainsKey(providerVersion.ProviderVersionId))
            {
                _providers[providerVersion.ProviderVersionId] = new Dictionary<string, Provider>();
            }

            providerVersion.Providers = _providers[providerVersion.ProviderVersionId].Values;
        }

        public void AddProviderToCoreProviderData(string providerVersionId, Provider provider)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.ArgumentNotNull(provider.ProviderId, nameof(provider.ProviderId));

            if (!_providers.ContainsKey(providerVersionId))
            {
                _providers.Add(providerVersionId, new Dictionary<string, Provider>());
            }

            _providers[providerVersionId][provider.ProviderId] = provider;
        }

        public void AddProviderAsScopedProvider(string specificationId, string providerVersionId, string providerId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            Dictionary<string, Provider> providerVersionProviders;
            if (!_providers.TryGetValue(providerVersionId, out providerVersionProviders))
            {
                throw new InvalidOperationException($"Provider Version not found. '{providerVersionId}'");
            }

            Provider provider;
            if (!providerVersionProviders.TryGetValue(providerId, out provider))
            {
                throw new InvalidOperationException($"Provider Version not found. Provider ID '{providerId}' in '{providerVersionId}'");
            }

            if (!_scopedProviders.ContainsKey(specificationId))
            {
                _scopedProviders.Add(specificationId, new Dictionary<string, Provider>());
            }

            _scopedProviders[specificationId][providerId] = provider;
        }
    }
}