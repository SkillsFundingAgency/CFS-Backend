namespace CalculateFunding.Services.Core.Constants
{
    public static class FunctionConstants
    {
        public const string PublishingApproveAllProviderFunding = "on-publishing-approve-all-provider-funding";
        
        public const string PublishingRunSqlImport = "on-publishing-run-sql-import";
        
        public const string PublishingRunSqlImportPoisoned = "on-publishing-run-sql-import-poisoned";

        public const string PublishIntegrityCheck = "on-publish-integrity-check";

        public const string PublishIntegrityCheckPoisoned = "on-publish-integrity-check-poisoned";

        public const string PublishingApproveAllProviderFundingPoisoned = "on-publishing-approve-all-provider-funding-poisoned";

        public const string PublishingApproveBatchProviderFunding = "on-publishing-approve-batch-provider-funding";

        public const string PublishingApproveBatchProviderFundingPoisoned = "on-publishing-approve-batch-provider-funding-poisoned";

        public const string PublishingPublishAllProviderFunding = "on-publishing-publish-all-provider-funding";

        public const string PublishingDatasetsDataCopy = "on-publishing-datasets-data-copy";

        public const string PublishingDatasetsDataCopyPoisoned = "on-publishing-datasets-data-copy-poisoned";

        public const string PublishingPublishAllProviderFundingPoisoned = "on-publishing-publish-all-provider-funding-poisoned";

        public const string PublishingPublishBatchProviderFunding = "on-publishing-publish-batch-provider-funding";

        public const string PublishingPublishBatchProviderFundingPoisoned = "on-publishing-publish-batch-provider-funding-poisoned";

        public const string PopulateScopedProviders = "on-populate-scopedproviders-event";

        public const string PopulateScopedProvidersPoisoned = "on-populate-scopedproviders-event-poisoned";

        public const string ProviderSnapshotDataLoad = "on-provider-snapshot-data-load";

        public const string ProviderSnapshotDataLoadPoisoned = "on-provider-snapshot-data-load-poisoned";

        public const string MapFdzDatasets = "on-map-fdz-datasets";

        public const string SearchIndexWriter = "on-search-index-writer";

        public const string MapFdzDatasetsPoisoned = "on-map-fdz-datasets-poisoned";

        public const string BatchPublishedProviderValidation = "on-batch-published-provider-validation";
        
        public const string BatchPublishedProviderValidationPoisoned = "on-batch-published-provider-validation-poisoned";

        public const string NewProviderVersionCheck = "on-new-provider-version-check";

        public const string TrackLatest = "on-track-latest";

        public const string TrackLatestPoisoned = "on-track-latest-poisoned";
    }
}
