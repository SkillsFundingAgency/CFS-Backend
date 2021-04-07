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
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class ProviderService : IProviderService
    {
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IPoliciesService _policiesService;
        private readonly IJobManagement _jobManagement;

        public ProviderService(IProvidersApiClient providersApiClient,
            IPoliciesService policiesService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies resiliencePolicies,
            IMapper mapper,
            IJobManagement jobManagement,
            ILogger logger)
        {
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _providersApiClient = providersApiClient;
            _policiesService = policiesService;
            _providersApiClientPolicy = resiliencePolicies.ProvidersApiClient;
            _publishedFundingDataService = publishedFundingDataService;
            _jobManagement = jobManagement;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            ApiResponse<ApiProviderVersion> providerVersionsResponse =
                await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            IEnumerable<Provider> Providers = _mapper.Map<IEnumerable<Provider>>(providerVersionsResponse.Content.Providers);

            return Providers ?? new Provider[0];
        }

        public async Task<IEnumerable<string>> GetScopedProviderIdsForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetScopedProviderIds(specificationId));

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
                await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetScopedProviderIds(specificationId));

            // fallback if redis cache has been invalidated queue a new scoped provider job
            if (scopedProviderIdResponse?.Content == null || scopedProviderIdResponse.Content.IsNullOrEmpty())
            {
                string correlationId = Guid.NewGuid().ToString();

                bool jobCompletedSuccessfully = await _jobManagement.QueueJobAndWait(async () =>
                {
                    ApiResponse<bool> refreshCacheFromApi = await _providersApiClientPolicy.ExecuteAsync(() =>
                                    _providersApiClient.RegenerateProviderSummariesForSpecification(specificationId, true));

                    if (!refreshCacheFromApi.StatusCode.IsSuccess())
                    {
                        string errorMessage = $"Unable to re-generate scoped providers doing refresh '{specificationId}' with status code: {refreshCacheFromApi.StatusCode}";
                    }

                    // returns true if job queued
                    return refreshCacheFromApi.Content;
                },
                JobConstants.DefinitionNames.PopulateScopedProvidersJob,
                specificationId,
                correlationId,
                ServiceBusConstants.TopicNames.JobNotifications);

                if (jobCompletedSuccessfully)
                {
                    scopedProviderIdResponse = await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetScopedProviderIds(specificationId));
                };

                if (scopedProviderIdResponse?.Content == null)
                {
                    return null;
                }
            }

            HashSet<string> scopedProviders = scopedProviderIdResponse?.Content.ToHashSet();

            IDictionary<string, Provider> scopedProvidersInVersion = (_mapper.Map<IEnumerable<Provider>>(providerVersionsResponse.Content.Providers.Where(p => scopedProviders.Contains(p.ProviderId)))).ToDictionary(_ => _.ProviderId);

            return scopedProvidersInVersion;
        }

        public async Task<IDictionary<string, PublishedProvider>> GenerateMissingPublishedProviders(IEnumerable<Provider> scopedProviders,
            SpecificationSummary specification,
            Reference fundingStream,
            IDictionary<string, PublishedProvider> publishedProviders)
        {
            string specificationId = specification?.Id;
            string specificationFundingPeriodId = await _policiesService.GetFundingPeriodId( specification?.FundingPeriod?.Id);
            string fundingStreamId = fundingStream?.Id;

            if (specificationId.IsNullOrWhitespace())
            {
                string error = "Could not locate a specification id on the supplied specification summary";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(specificationId));
            }

            if (specificationFundingPeriodId.IsNullOrWhitespace())
            {
                string error = "Could not locate a funding period id on the supplied specification summary";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(specificationFundingPeriodId));
            }

            if (fundingStreamId.IsNullOrWhitespace())
            {
                string error = "Could not locate a funding stream id from the supplied reference";
                _logger.Error(error);
                throw new ArgumentOutOfRangeException(nameof(fundingStreamId));
            }

            return scopedProviders.Where(_ =>
                    !publishedProviders.ContainsKey($"{_.ProviderId}"))
                .Select(_ => CreatePublishedProvider(_, specificationFundingPeriodId, fundingStreamId, specificationId, "Add by system because published provider doesn't already exist"))
                .ToDictionary(_ => _.Current.ProviderId, _ => _);
        }

        public async Task<(IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream, IDictionary<string, PublishedProvider> ScopedPublishedProviders)> 
            GetPublishedProviders(Reference fundingStream, SpecificationSummary specification)
        {
            IDictionary<string, PublishedProvider> publishedProvidersForFundingStream = await GetPublishedProvidersForFundingStream(fundingStream, specification);

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

            foreach (Provider predecessor in scopedProviders.Values.Where(_ => _.GetSuccessors().Any()))
            {
                foreach (string successor in predecessor.GetSuccessors())
                {
                    if (!scopedPublishedProviders.ContainsKey(successor))
                    {
                        if (publishedProviders.ContainsKey(successor))
                        {
                            scopedPublishedProviders.Add(successor, publishedProviders[successor]);
                        }
                        else
                        {
                            string error = $"Could not locate the successor provider:{successor}";
                            _logger.Error(error);
                            throw new ArgumentOutOfRangeException("Successor");
                        }
                    }    
                }
            }

            return scopedPublishedProviders;
        }

        private async Task<IDictionary<string, PublishedProvider>> GetPublishedProvidersForFundingStream(
            Reference fundingStream, SpecificationSummary specification)
        {
            _logger.Information($"Retrieving published provider results for {fundingStream.Id} in specification {specification.Id}");
            
            IEnumerable<PublishedProvider> publishedProvidersResult =
                await _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, specification.FundingPeriod.Id);

            // Ensure linq query evaluates only once
            Dictionary<string, PublishedProvider> publishedProvidersForFundingStream = publishedProvidersResult.ToDictionary(_ => _.Current.ProviderId);
            _logger.Information($"Retrieved {publishedProvidersForFundingStream.Count} published provider results for {fundingStream.Id}");


            if (publishedProvidersForFundingStream.IsNullOrEmpty())
                throw new RetriableException($"Null or empty published providers returned for specification id : '{specification.Id}' when setting status to released");

            return publishedProvidersForFundingStream;
        }

        public async Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(PublishedProvider predecessor, string successorId, string providerVersionId)
        {
            ApiResponse<ProviderVersionSearchResult> apiResponse = await _providersApiClientPolicy.ExecuteAsync(() =>
                _providersApiClient.GetProviderByIdFromProviderVersion(providerVersionId, successorId));

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