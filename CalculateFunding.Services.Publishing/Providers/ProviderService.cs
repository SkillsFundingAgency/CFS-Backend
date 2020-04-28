using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using Polly;
using AutoMapper;
using ApiProviderVersion = CalculateFunding.Common.ApiClient.Providers.Models.ProviderVersion;
using PublishedProvider = CalculateFunding.Models.Publishing.PublishedProvider;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class ProviderService : IProviderService
    {
        private readonly IProvidersApiClient _providers;
        private readonly AsyncPolicy _resiliencePolicy;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public ProviderService(IProvidersApiClient providers,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies resiliencePolicies,
            IMapper mapper,
            ILogger logger)
        {
            Guard.ArgumentNotNull(providers, nameof(providers));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _providers = providers;
            _resiliencePolicy = resiliencePolicies.ProvidersApiClient;
            _publishedFundingDataService = publishedFundingDataService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            ApiResponse<ApiProviderVersion> providerVersionsResponse =
                await _resiliencePolicy.ExecuteAsync(() => _providers.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            IEnumerable<Provider> Providers = _mapper.Map<IEnumerable<Provider>>(providerVersionsResponse.Content.Providers);

            return Providers ?? new Provider[0];
        }

        public async Task<IEnumerable<string>> GetScopedProviderIdsForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _resiliencePolicy.ExecuteAsync(() => _providers.GetScopedProviderIds(specificationId));

            if (scopedProviderIdResponse == null)
            {
                throw new InvalidOperationException("Scoped provider response was null");
            }

            if (scopedProviderIdResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new RetriableException($"Scoped provider response was not OK, but instead '{scopedProviderIdResponse.StatusCode}'");
            }

            if (scopedProviderIdResponse.Content == null)
            {
                throw new InvalidOperationException("Scoped provider response content was null");
            }

            return scopedProviderIdResponse.Content;
        }

        public async Task<IDictionary<string, Provider>> GetScopedProvidersForSpecification(string specificationId, string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            ApiResponse<ApiProviderVersion> providerVersionsResponse =
                await _resiliencePolicy.ExecuteAsync(() => _providers.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _resiliencePolicy.ExecuteAsync(() => _providers.GetScopedProviderIds(specificationId));

            Guard.ArgumentNotNull(scopedProviderIdResponse?.Content, nameof(scopedProviderIdResponse));

            HashSet<string> scopedProviders = scopedProviderIdResponse?.Content.ToHashSet();

            IDictionary<string, Provider> scopedProvidersInVersion = (_mapper.Map<IEnumerable<Provider>>(providerVersionsResponse.Content.Providers.Where(p => scopedProviders.Contains(p.ProviderId)))).ToDictionary(_ => _.ProviderId);

            return scopedProvidersInVersion;
        }

        public IDictionary<string, PublishedProvider> GenerateMissingPublishedProviders(IEnumerable<Provider> scopedProviders,
            SpecificationSummary specification,
            Reference fundingStream,
            IDictionary<string, PublishedProvider> publishedProviders)
        {
            string specificationId = specification?.Id;
            string fundingPeriodId = specification?.FundingPeriod?.Id;
            string fundingStreamId = fundingStream?.Id;

            if (specificationId.IsNullOrWhitespace())
            {
                string error = "Could not locate a specification id on the supplied specification summary";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(specificationId));
            }

            if (fundingPeriodId.IsNullOrWhitespace())
            {
                string error = "Could not locate a funding period id on the supplied specification summary";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(fundingPeriodId));
            }

            if (fundingStreamId.IsNullOrWhitespace())
            {
                string error = "Could not locate a funding stream id from the supplied reference";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(fundingStreamId));
            }

            return scopedProviders.Where(_ =>
                    !publishedProviders.ContainsKey($"{_.ProviderId}"))
                .Select(_ => CreatePublishedProvider(_,fundingPeriodId, fundingStreamId, specificationId, "Add by system because published provider doesn't already exist"))
                .ToDictionary(_ => _.Current.ProviderId, _ => _);
        }

        public async Task<(IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream, IDictionary<string, PublishedProvider> ScopedPublishedProviders)> 
            GetPublishedProviders(Reference fundingStream, SpecificationSummary specification, string[] providerIds = null)
        {
            IDictionary<string, PublishedProvider> publishedProvidersForFundingStream = await GetPublishedProvidersForFundingStream(fundingStream, specification, providerIds);

            IDictionary<string, Provider> scopedProviders =
                await GetScopedProvidersForSpecification(specification.Id, specification.ProviderVersionId);

            IDictionary<string, PublishedProvider> scopedPublishedProvidersForFundingStream = ReadScopedProviders(publishedProvidersForFundingStream, scopedProviders);

            return (publishedProvidersForFundingStream, scopedPublishedProvidersForFundingStream);
        }

        private IDictionary<string, PublishedProvider> ReadScopedProviders(IDictionary<string, PublishedProvider> publishedProviders,
            IDictionary<string, Provider> scopedProviders
            )
        {
            IDictionary<string, PublishedProvider> scopedPublishedProviders = publishedProviders.Values.Where(_ => scopedProviders.ContainsKey(_.Current.Provider.ProviderId)).ToDictionary(_ => _.Current.Provider.ProviderId);

            foreach (Provider predessor in scopedProviders.Values.Where(_ =>  !string.IsNullOrWhiteSpace(_.Successor)))
            {
                if (!scopedPublishedProviders.ContainsKey(predessor.Successor))
                {
                    scopedPublishedProviders.Add(predessor.Successor, publishedProviders[predessor.Successor]);
                }
            }

            return scopedPublishedProviders;
        }

        private async Task<IDictionary<string, PublishedProvider>> GetPublishedProvidersForFundingStream(
            Reference fundingStream, SpecificationSummary specification, string[] providerIds = null)
        {
            _logger.Information($"Retrieving published provider results for {fundingStream.Id} in specification {specification.Id}");

            IEnumerable<PublishedProvider> publishedProvidersResult =
                await _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, specification.FundingPeriod.Id, providerIds);

            // Ensure linq query evaluates only once
            Dictionary<string, PublishedProvider> publishedProvidersForFundingStream = publishedProvidersResult.ToDictionary(_ => _.Current.ProviderId);
            _logger.Information($"Retrieved {publishedProvidersForFundingStream.Count} published provider results for {fundingStream.Id}");


            if (publishedProvidersForFundingStream.IsNullOrEmpty())
                throw new RetriableException($"Null or empty published providers returned for specification id : '{specification.Id}' when setting status to released");

            return publishedProvidersForFundingStream;
        }

        public async Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(PublishedProvider predecessor, string successorId, string providerVersionId)
        {
            ApiResponse<ProviderVersionSearchResult> apiResponse = await _resiliencePolicy.ExecuteAsync(() =>
                _providers.GetProviderByIdFromProviderVersion(providerVersionId, successorId));

            ProviderVersionSearchResult providerVersionSearchResult = apiResponse?.Content;

            if (providerVersionSearchResult == null)
            {
                return null;
            }

            PublishedProviderVersion predecessorProviderVersion = predecessor.Current;

            PublishedProvider missingProvider = CreatePublishedProvider(_mapper.Map<Provider>(providerVersionSearchResult),
                predecessorProviderVersion.FundingPeriodId,
                predecessorProviderVersion.FundingStreamId,
                predecessorProviderVersion.SpecificationId,
                "Created by the system as not in scope but referenced as a successor provider",
                predecessorProviderVersion.FundingLines.DeepCopy(),
                predecessorProviderVersion.Calculations.DeepCopy());

            foreach (ProfilePeriod profilePeriod in missingProvider.Current.FundingLines.SelectMany(_ =>
                _.DistributionPeriods.SelectMany(dp => dp.ProfilePeriods)))
            {
                profilePeriod.ProfiledValue = 0;
            }

            return missingProvider;
        }

        private PublishedProvider CreatePublishedProvider(Provider provider, 
            string fundingPeriod, 
            string fundingStreamId, 
            string specificationId, 
            string comment,
            IEnumerable<FundingLine> fundingLines = null,
            IEnumerable<FundingCalculation> fundingCalculations = null)
        {
            return new PublishedProvider
            {
                Current = new PublishedProviderVersion
                {
                    Version = 1,
                    MajorVersion = 0,
                    MinorVersion = 1,
                    FundingPeriodId = fundingPeriod,
                    FundingStreamId = fundingStreamId,
                    ProviderId = provider.ProviderId,
                    Provider = provider,
                    SpecificationId = specificationId,
                    Comment = comment,
                    FundingLines = fundingLines,
                    Calculations = fundingCalculations
                }
            };
        }
    }
}