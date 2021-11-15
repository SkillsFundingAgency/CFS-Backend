using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Variations;

namespace CalculateFunding.Services.Publishing.Models
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ProviderVariationContext
    {
        private readonly IPoliciesService _policiesService;
        private readonly Queue<IVariationChange> _variationChanges = new Queue<IVariationChange>();

        public ProviderVariationContext(IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            _policiesService = policiesService;
            OrganisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>();
        }

        public IList<string> ApplicableVariations;

        public IEnumerable<string> Variances;

        public string ProviderId => RefreshState?.ProviderId;

        /// <summary>
        /// Latest (current) core provider information which is being compared against the prior provider information
        /// </summary>
        public Provider UpdatedProvider { get; set; }

        public decimal? UpdatedTotalFunding { get; set; }
        
        public IEnumerable<ProfileVariationPointer> VariationPointers { get; set; }

        public IEnumerable<string> AffectedFundingLineCodes(string strategy) => 
            _affectedFundingLines != null && _affectedFundingLines.ContainsKey(strategy) ? 
            _affectedFundingLines[strategy] : 
            null;

        public IEnumerable<string> AllAffectedFundingLineCodes => _affectedFundingLines?.Values.SelectMany(_ => _);

        private ConcurrentDictionary<string, List<string>> _affectedFundingLines;

        public void AddAffectedFundingLineCode(string strategy, string fundingLineCode)
        {
            _affectedFundingLines ??= new ConcurrentDictionary<string, List<string>>();

            _affectedFundingLines.AddOrUpdate(strategy, 
                new List<string> { fundingLineCode }, 
                (k,v) => {
                    v.Add(fundingLineCode);
                    return v;
                });
        }
        
        public ICollection<PublishedProvider> NewProvidersToAdd { get; } = new List<PublishedProvider>(); 

        /// <summary>
        /// The calling code uses side effects on the PublishedProvider.Current instance
        /// to map changes made during refresh into a new published provider downstream
        /// so changes in the variations should alter the state of this PublishedProviderVersion
        /// </summary>
        public PublishedProviderVersion RefreshState => PublishedProvider?.Current;


        public PublishedProvider Successor { get; set; }

        /// <summary>
        /// The variation detection should compare the updated provider with the last published details
        /// here using the latest released publishedproviderversion
        /// </summary>
        public PublishedProviderVersion ReleasedState => PublishedProvider?.Released;

        /// <summary>
        /// The publisher provider version to use as the comparison for previous state
        /// this is the currently released version 
        /// </summary>
        public PublishedProviderVersion PriorState => GetPublishedProviderOriginalSnapShot(ProviderId)?.Released;

        /// <summary>
        /// The publisher provider version to use as the comparison for previous state
        /// this is the current version 
        /// </summary>
        public PublishedProviderVersion CurrentState => GetPublishedProviderOriginalSnapShot(ProviderId)?.Current;

        public PublishedProvider AddMissingProvider(PublishedProvider missingProvider)
        {
            if (missingProvider != null)
            {
                NewProvidersToAdd.Add(missingProvider);

                string providerId = missingProvider.Current.ProviderId;

                AllPublishedProvidersRefreshStates[providerId] = missingProvider;
                AllPublishedProviderSnapShots[providerId] = new PublishedProviderSnapShots(missingProvider);

                return missingProvider;
            }
            else
            {
                return null;
            }
        }

        public PublishedProvider PublishedProvider { get; set; }
        
        /// <summary>
        /// We take a snapshot of the published providers post any changes from the refresh run
        /// so should be used as a readonly reference when running variations
        /// </summary>
        public IDictionary<string, PublishedProviderSnapShots> AllPublishedProviderSnapShots { get; set; }
        
        /// <summary>
        /// Some variations require alterations to other providers. The instances set as the RefreshState
        /// on the variation context come from this master list and its these instances that should be updated
        /// in any queued changes (e.g. for a merge - we close one provider which is the RefreshState on the context
        /// but then we also then have to transfer the money we zero off out remaining profiles onto our successor
        /// provider - you would locate the refresh state instance of the successor provider from this lookup
        /// </summary>
        public IDictionary<string, PublishedProvider> AllPublishedProvidersRefreshStates { get; set; }

        public ICollection<string> ErrorMessages { get; } = new List<string>();
        
        public ICollection<VariationReason> VariationReasons { get; set; } = new List<VariationReason>();

        public void AddVariationReasons(params VariationReason[] variationReasons)
        {
            VariationReasons.AddRange(variationReasons.Except(VariationReasons));
        }

        public void RecordErrors(params string[] errors)
        {
            ErrorMessages.AddRange(errors);
        }
        
        /// <summary>
        /// Get a readonly snapshot of the prior state (before this refresh run) of the published provider
        /// </summary>
        public PublishedProvider GetPublishedProviderOriginalSnapShot(string providerId)
        {
            return AllPublishedProviderSnapShots.TryGetValue(providerId, out PublishedProviderSnapShots publishedProvider)
                ? publishedProvider.Original
                : null;       
        }

        /// <summary>
        /// Get a readonly snapshot of the published provider with the latest funding lines
        /// from the refresh run but that will not be altered during variations
        /// </summary>
        public PublishedProvider GetPublishedProviderRefreshedSnapShot(string providerId)
        {
            return AllPublishedProviderSnapShots.TryGetValue(providerId, out PublishedProviderSnapShots publishedProvider)
                ? publishedProvider.LatestSnapshot
                : null;       
        }

        /// <summary>
        /// Get the instance of the published provider that is being updated during refresh
        /// </summary>
        public PublishedProvider GetPublishedProviderRefreshState(string providerId)
        {
            return AllPublishedProvidersRefreshStates.TryGetValue(providerId, out PublishedProvider publishedProvider)
                ? publishedProvider
                : null;         
        }

        public void QueueVariationChange(IVariationChange variationChange)
        {
            //the different concrete variation strategies can queue implementations 
            //of IVariationChange here to be run against this provider context
            //the changes are basically commands to run (moving funding about, creating new providers etc.)
            //the strategies instantiate the correct change and set its properties correctly so that it
            //can be run in order per provider
            
            _variationChanges.Enqueue(variationChange);    
        }

        public virtual async Task ApplyVariationChanges(IApplyProviderVariations variationsApplication)
        {
            while (_variationChanges.Count > 0)
            {
                IVariationChange variationChange = _variationChanges.Dequeue();

                await variationChange.Apply(variationsApplication);
            }
        }

        public IEnumerable<IVariationChange> QueuedChanges => _variationChanges.ToArray();

        public bool HasVariationChanges => _variationChanges.AnyWithNullCheck();

        public bool HasNewProvidersToAdd => NewProvidersToAdd.AnyWithNullCheck();

        public IEnumerable<string> AffectedFundingLinesWithVariationPointerSet(string strategy) => AffectedFundingLineCodes(strategy)?.Where(_ => (VariationPointers?.Any(vp => vp.FundingLineId == _)).GetValueOrDefault());

        /// <summary>
        /// Provider version ID of the existing specificaton which will be used to search provider from core provider data
        /// </summary>
        public string ProviderVersionId { get; set; }
        
        /// <summary>
        /// The funding configuration for this spec-funding stream-provider combination
        /// </summary>
        public FundingConfiguration FundingConfiguration { get; set; }

        public async Task<string> GetPriorStateSchemaVersion() => PriorState == null ? null : await GetSchemaVersion(PriorState.FundingStreamId, PriorState.FundingPeriodId, PriorState.TemplateVersion);
        public async Task<string> GetReleasedStateSchemaVersion() => ReleasedState == null ? null : await GetSchemaVersion(ReleasedState.FundingStreamId, ReleasedState.FundingPeriodId, ReleasedState.TemplateVersion);

        public IDictionary<string, IEnumerable<OrganisationGroupResult>> OrganisationGroupResultsData { get; set; }

        public static string OrganisationGroupsKey(string fundingStreamId, string fundingPeriodId) => $"{fundingStreamId}:{fundingPeriodId}";

        private readonly IDictionary<string, string> schemaVersions = new Dictionary<string, string>();

        private async Task<string> GetSchemaVersion(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            if(string.IsNullOrWhiteSpace(fundingStreamId) && string.IsNullOrWhiteSpace(fundingPeriodId) && string.IsNullOrWhiteSpace(templateVersion))
            {
                return null;
            }

            string schemaVersionKey = $"{fundingStreamId}-{fundingPeriodId}-{templateVersion}".ToLower();

            if (!schemaVersions.ContainsKey(schemaVersionKey))
            {
                TemplateMetadataContents templateContents = await _policiesService.GetTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
                schemaVersions[schemaVersionKey] = templateContents?.SchemaVersion;
            }

            return schemaVersions[schemaVersionKey];
        }
    }
}
