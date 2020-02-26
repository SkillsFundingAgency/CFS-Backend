using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class OutOfScopePublishedProviderBuilder : IOutOfScopePublishedProviderBuilder
    {
        private readonly Policy _resilience;
        private readonly IProvidersApiClient _providers;
        private readonly IMapper _mapper;

        public OutOfScopePublishedProviderBuilder(IProvidersApiClient providers,
            IPublishingResiliencePolicies resiliencePolicies, 
            IMapper mapper)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(providers, nameof(providers));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            
            _resilience = resiliencePolicies.ProvidersApiClient;
            _providers = providers;
            _mapper = mapper;
        }
        
        public async Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(ProviderVariationContext providerVariationContext,
            string successorId)
        {
            ApiResponse<ProviderVersionSearchResult> apiResponse = await _resilience.ExecuteAsync(() =>
                _providers.GetProviderByIdFromMaster(successorId));

            ProviderVersionSearchResult providerVersionSearchResult = apiResponse?.Content;
            
            if (providerVersionSearchResult == null)
            {
                return null;
            }
            
            PublishedProvider predecessor = providerVariationContext.PublishedProvider;
            PublishedProviderVersion predecessorProviderVersion = predecessor.Current;
            
            PublishedProvider missingProvider = new PublishedProvider
            {
                Current = new PublishedProviderVersion
                {
                    Provider = _mapper.Map<Provider>(providerVersionSearchResult),
                    Author = new Reference
                    {
                        Name = "system"
                    },
                    Date = DateTimeOffset.UtcNow,
                    ProviderId = successorId,
                    SpecificationId = predecessorProviderVersion.SpecificationId,
                    FundingPeriodId = predecessorProviderVersion.FundingPeriodId,
                    FundingStreamId = predecessorProviderVersion.FundingStreamId,
                    FundingLines = predecessorProviderVersion.FundingLines.DeepCopy(),
                    Comment = "Created by the system as not in scope but referenced as a successor provider"
                }
            };

            foreach (ProfilePeriod profilePeriod in missingProvider.Current.FundingLines.SelectMany(_ => 
                _.DistributionPeriods.SelectMany(dp => dp.ProfilePeriods)))
            {
                profilePeriod.ProfiledValue = 0;
            }

            providerVariationContext.AddMissingProvider(missingProvider);
            
            return missingProvider;
        }
    }
}