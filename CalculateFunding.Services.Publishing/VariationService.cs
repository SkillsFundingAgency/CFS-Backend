using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class VariationService : IVariationService
    {
        private readonly IDetectProviderVariations _detectProviderVariations;
        private readonly IApplyProviderVariations _applyProviderVariations;
        private readonly ConcurrentDictionary<string, IDictionary<string, PublishedProviderSnapShots>> _snapshots;
        private readonly IRecordVariationErrors _recordVariationErrors;
        private readonly ILogger _logger;

        public int ErrorCount => _applyProviderVariations.ErrorMessages.Count();

        public VariationService(IDetectProviderVariations detectProviderVariations,
            IApplyProviderVariations applyProviderVariations,
            IRecordVariationErrors recordVariationErrors,
            ILogger logger)
        {
            Guard.ArgumentNotNull(detectProviderVariations, nameof(detectProviderVariations));
            Guard.ArgumentNotNull(applyProviderVariations, nameof(applyProviderVariations));
            Guard.ArgumentNotNull(recordVariationErrors, nameof(recordVariationErrors));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _detectProviderVariations = detectProviderVariations;
            _applyProviderVariations = applyProviderVariations;
            _recordVariationErrors = recordVariationErrors;
            _logger = logger;
            _snapshots = new ConcurrentDictionary<string, IDictionary<string, PublishedProviderSnapShots>>();
        }

        public string SnapShot(IDictionary<string, PublishedProvider> publishedProviders,
            string snapshotId = null)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            snapshotId ??= Guid.NewGuid().ToString();

            IDictionary<string, PublishedProviderSnapShots> snapshots = publishedProviders
                    .ToDictionary(_ => _.Key, _ => new PublishedProviderSnapShots(_.Value));

            // only add the snapshot if there are published providers
            if (snapshots.Count > 0)
            {
                _snapshots.AddOrUpdate(snapshotId, snapshots, (k, v) => snapshots);
            }

            return snapshotId;

        }

        public async Task<ProviderVariationContext> PrepareVariedProviders(decimal? updatedTotalFunding,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            PublishedProvider existingPublishedProvider,
            Provider updatedProvider,
            IEnumerable<FundingVariation> variations,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string snapshotId,
            string specificationProviderVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData,
            IEnumerable<string> variances,
            string fundingStreamId,
            string fundingPeriodId,
            PublishedProviderVersion preRefreshProviderVersion)
        {
            Guard.ArgumentNotNull(updatedTotalFunding, nameof(updatedTotalFunding));
            Guard.ArgumentNotNull(allPublishedProviderRefreshStates, nameof(allPublishedProviderRefreshStates));
            Guard.ArgumentNotNull(existingPublishedProvider, nameof(existingPublishedProvider));
            Guard.ArgumentNotNull(updatedProvider, nameof(updatedProvider));

            bool shouldRunVariations = variations.AnyWithNullCheck() &&
                                        !string.IsNullOrWhiteSpace(snapshotId);

            _logger.Verbose($"Variations enabled = {shouldRunVariations}");

            if (!shouldRunVariations || !_snapshots.TryGetValue(snapshotId, out IDictionary<string, PublishedProviderSnapShots> publishedProviderSnapshots))
            {
                return null;
            }

            _logger.Verbose($"Number of snapshot providers = {publishedProviderSnapshots.Count}");

            ProviderVariationContext variationContext = await _detectProviderVariations.CreateRequiredVariationChanges(existingPublishedProvider,
                updatedTotalFunding,
                updatedProvider,
                variations,
                publishedProviderSnapshots,
                allPublishedProviderRefreshStates,
                variationPointers,
                specificationProviderVersionId,
                organisationGroupResultsData,
                fundingStreamId,
                fundingPeriodId,
                preRefreshProviderVersion,
                variances);

            if (variationContext.HasVariationChanges)
            {
                _applyProviderVariations.AddVariationContext(variationContext);
            }

            return variationContext;
        }

        public async Task<bool> ApplyVariations(IRefreshStateService refreshStateService,
            string specificationId,
            string jobId)
        {
            Guard.ArgumentNotNull(refreshStateService, nameof(refreshStateService));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (_applyProviderVariations.HasVariations)
            {
                await _applyProviderVariations.ApplyProviderVariations();

                if (_applyProviderVariations.HasErrors)
                {
                    await _recordVariationErrors.RecordVariationErrors(_applyProviderVariations.ErrorMessages, specificationId, jobId);

                    _logger.Error("Unable to complete refresh funding job. Encountered variation errors");

                    return false;
                }

                foreach (PublishedProvider publishedProvider in _applyProviderVariations.ProvidersToUpdate)
                {
                    refreshStateService.Update(publishedProvider);
                }

                foreach (PublishedProvider publishedProvider in _applyProviderVariations.NewProvidersToAdd)
                {
                    refreshStateService.Add(publishedProvider);
                }
            }

            return true;
        }

        public void ClearSnapshots()
        {
            _snapshots.Clear();
        }
    }
}
