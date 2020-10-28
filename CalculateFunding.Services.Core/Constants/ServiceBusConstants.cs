namespace CalculateFunding.Services.Core.Constants
{
    public static class ServiceBusConstants
    {
        public const string ConnectionStringConfigurationKey = "ServiceBusSettings:ConnectionString";

        public static class QueueNames
        {
            public const string ApplyTemplateCalculations = "apply-template-calculations";

            public const string ApplyTemplateCalculationsPoisoned = "apply-template-calculations/$DeadLetterQueue";

            public const string ApplyTemplateCalculationsPoisonedLocal = "apply-template-calculations-poisoned";

            public const string CalculationJobInitialiser = "calc-events-instruct-generate-allocations";

            public const string CalculationJobInitialiserPoisoned = "calc-events-instruct-generate-allocations/$DeadLetterQueue";

            public const string CalculationJobInitialiserPoisonedLocal = "calc-events-instruct-generate-allocations-poisoned";

            public const string CalcEngineGenerateAllocationResults = "calc-events-generate-allocations-results";

            public const string CalcEngineGenerateAllocationResultsPoisoned = "calc-events-generate-allocations-results/$DeadLetterQueue";

            public const string CalcEngineGenerateAllocationResultsPoisonedLocal = "calc-events-generate-allocations-results-poisoned";

            public const string TestEngineExecuteTests = "test-events-execute-tests";

            public const string AddDefinitionRelationshipToSpecification = "spec-events-add-definition-relationship";

            public const string ProcessDataset = "dataset-events-datasets";

            public const string MapFdzDatasets = "map-fdz-datasets";

            public const string MapFdzDatasetsPoisoned = "map-fdz-datasets/$DeadLetterQueue";

            public const string MapFdzDatasetsPoisonedLocal = "map-fdz-datasets-poisoned";

            public const string ProcessDatasetPoisoned = "dataset-events-datasets/$DeadLetterQueue";

            public const string ProcessDatasetPoisonedLocal = "dataset-events-datasets-poisoned";

            public const string ValidateDataset = "dataset-validate";

            public const string ValidateDatasetPoisoned = "dataset-validate/$DeadLetterQueue";

            public const string ValidateDatasetPoisonedLocal = "dataset-validate-poisoned";

            public const string CreateDraftCalculation = "calc-events-create-draft";

            public const string UpdateBuildProjectRelationships = "calc-events-add-relationship-to-buildproject";

            public const string PublishingApproveAllProviderFunding = "publishing-approve-all-provider-funding";

            public const string PublishIntegrityCheck = "publish-integrity-check";

            public const string PublishIntegrityCheckPoisoned = "publish-integrity-check/$DeadLetterQueue";

            public const string PublishIntegrityCheckPoisonedLocal = "publish-integrity-check-poisoned";

            public const string PublishingApproveAllProviderFundingPoisoned = "publishing-approve-all-provider-funding/$DeadLetterQueue";

            public const string PublishingApproveAllProviderFundingPoisonedLocal = "publishing-approve-all-provider-funding-poisoned";

            public const string PublishingApproveBatchProviderFunding = "publishing-approve-batch-provider-funding";
                                                 
            public const string PublishingApproveBatchProviderFundingPoisoned = "publishing-approve-batch-provider-funding/$DeadLetterQueue";

            public const string PublishingApproveBatchProviderFundingPoisonedLocal = "publishing-approve-batch-provider-funding-poisoned";

            public const string PublishingRefreshFunding = "publishing-refreshfunding";

            public const string PublishingRefreshFundingPoisoned = "publishing-refreshfunding/$DeadLetterQueue";

            public const string PublishingRefreshFundingPoisonedLocal = "publishing-refreshfunding-poisoned";

            public const string PublishingPublishAllProviderFunding = "publishing-publish-all-provider-funding";

            public const string PublishingPublishAllProviderFundingPoisoned = "publishing-publish-all-provider-funding/$DeadLetterQueue";

            public const string PublishingPublishAllProviderFundingPoisonedLocal = "publishing-publish-all-provider-funding-poisoned";

            public const string PublishingPublishBatchProviderFunding = "publishing-publish-batch-provider-funding";

            public const string PublishingPublishBatchProviderFundingPoisoned = "publishing-publish-batch-provider-funding/$DeadLetterQueue";

            public const string PublishingPublishBatchProviderFundingPoisonedLocal = "publishing-publish-batch-provider-funding-poisoned";

            public const string ReIndexCalculationResultsIndex = "reindex-calculation-results-index";

            public const string CalculationResultsCsvGeneration = "calculation-results-csv-generation";

            //For debug queue only as its on a timer
            public const string ScaleDownCosmosdbCollection = "scale-down-cosmosdb-collection";

            public const string IncrementalScaleDownCosmosdbCollection = "incremental-scale-down-cosmosdb-collection";

            public const string CalculationResultsCsvGenerationTimer = "calculation-results-csv-generation-timer";
            
            public const string PublishingReIndexPublishedProviders = "publishing-reindex-published-providers";
            
            public const string PublishingReIndexPublishedProvidersPoisoned = "publishing-reindex-published-providers/$DeadLetterQueue";
            
            public const string PublishingReIndexPublishedProvidersPoisonedLocal = "publishing-reindex-published-providers-poisoned";

            public const string PopulateScopedProviders = "populate-scopedproviders";

            public const string PopulateScopedProvidersPoisoned = "populate-scopedproviders/$DeadLetterQueue";

            public const string PopulateScopedProvidersPoisonedLocal = "populate-scopedproviders-poisoned";

            public const string DeleteCalculations = "calculations-delete";

            public const string DeleteCalculationsPoisoned = "calculations-delete/$DeadLetterQueue";

            public const string DeleteCalculationsPoisonedLocal = "calculations-delete-poisoned";

            public const string DeleteCalculationResults = "calculation-results-delete";

            public const string DeleteCalculationResultsPoisoned = "calculation-results-delete/$DeadLetterQueue";

            public const string DeleteCalculationResultsPoisonedLocal = "calculation-results-delete-poisoned";

            public const string DeleteDatasets = "datasets-delete";

            public const string DeleteDatasetsPoisoned = "datasets-delete/$DeadLetterQueue";

            public const string DeleteDatasetsPoisonedLocal = "datasets-delete-poisoned";

            public const string DeleteTestResults = "test-results-delete";

            public const string DeleteTestResultsPoisoned = "test-results-delete/$DeadLetterQueue";

            public const string DeleteTestResultsPoisonedLocal = "test-results-delete-poisoned";

            public const string DeleteTests = "tests-delete";

            public const string DeleteTestsPoisoned = "tests-delete/$DeadLetterQueue";

            public const string DeleteTestsPoisonedLocal = "tests-delete-poisoned";

            public const string DeleteSpecifications = "specifications-delete";

            public const string DeleteSpecificationsPoisoned = "specifications-delete/$DeadLetterQueue";

            public const string DeleteSpecificationsPoisonedLocal = "specifications-delete-poisoned";

            public const string ReIndexSingleSpecification = "publishing-reindex-specification";
            
            public const string DeletePublishedProviders = "publishing-delete-published-providers";

            public const string DeletePublishedProvidersPoisoned = "publishing-delete-published-providers/$DeadLetterQueue";

            public const string DeletePublishedProvidersPoisonedLocal = "publishing-delete-published-providers-poisoned";

            public const string ReIndexSpecificationCalculationRelationships = "calculations-reindex-specification-calculation-rels";

            public const string ReIndexSpecificationCalculationRelationshipsPoisoned = "calculations-reindex-specification-calculation-rels/$DeadLetterQueue";
            
            public const string ReIndexSpecificationCalculationRelationshipsPoisonedLocal = "calculations-reindex-specification-calculation-rels-poisoned";

            public const string GeneratePublishedFundingCsv = "publishing-generate-published-funding-csv";
            
            public const string GeneratePublishedFundingCsvPoisoned = "publishing-generate-published-funding-csv/$DeadLetterQueue";
            
            public const string GeneratePublishedFundingCsvPoisonedLocal = "publishing-generate-published-funding-csv-poisoned";

            public const string GeneratePublishedProviderEstateCsv = "publishing-generate-published-provider-estate-csv";
                                                 
            public const string GeneratePublishedProviderEstateCsvPoisoned = "publishing-generate-published-provider-estate-csv/$DeadLetterQueue";
                                                 
            public const string GeneratePublishedProviderEstateCsvPoisonedLocal = "publishing-generate-published-provider-estate-csv-poisoned";
            
            public const string PublishedFundingUndo = "publishing-published-funding-undo";

            public const string PolicyReIndexTemplates = "policy-reindex-templates";

            public const string PolicyReIndexTemplatesPoisoned = "policy-reindex-templates/$DeadLetterQueue";

            public const string PolicyReIndexTemplatesPoisonedLocal = "policy-reindex-templates-poisoned";

            public const string MergeSpecificationInformationForProvider = "merge-specification-information-for-provider-with-results";
            
            public const string MergeSpecificationInformationForProviderPoisoned = "merge-specification-information-for-provider-with-results/$DeadLetterQueue";
            
            public const string MergeSpecificationInformationForProviderPoisonedLocal = "merge-specification-information-poisoned";

            public const string ProviderSnapshotDataLoad = "provider-snapshot-data-load";

            public const string ProviderSnapshotDataLoadPoisoned = "provider-snapshot-data-load/$DeadLetterQueue";

            public const string ProviderSnapshotDataLoadPoisonedLocal = "provider-snapshot-data-load-poisoned";

            public const string UpdateCodeContextCache = "code-context-update";
            
            public const string UpdateCodeContextCachePoisoned = "code-context-update/$DeadLetterQueue";
            
            public const string UpdateCodeContextCachePoisonedLocal = "code-context-update-poisoned";

            public const string SearchIndexWriter = "search-index-writer";

            public const string SearchIndexWriterPoisoned = "search-index-writer/$DeadLetterQueue";
            
            public const string SearchIndexWriterPoisonedLocal = "search-index-writer-poisoned";
        }

        public static class TopicNames
        {
            public const string EditSpecification = "edit-specification";

            public const string EditCalculation = "edit-calculation";

            public const string JobNotifications = "job-notifications";

            public const string DataDefinitionChanges = "data-definition-changes";

            public const string ProviderSourceDatasetCleanup = "provider-sourcedataset-cleanup";

            public const string SmokeTest = "smoketest";
        }

        public static class TopicSubscribers
        {
            public const string UpdateCalculationsForEditSpecification = "calculation-update";

            public const string UpdateScenariosForEditSpecification = "test-scenario-update";

            public const string UpdateScenarioResultsForEditSpecification = "test-scenario-result-update";

            public const string CleanupTestResultsForSpecificationProviders = "test-specification-provider-results-cleanup";

            public const string UpdateUsersForEditSpecification = "users-update";

            public const string UpdateScenariosForEditCalculation = "test-scenario-update";

            public const string UpdateCalculationsForEditCalculation = "calcs-calculation-update";

            public const string UpdateJobsOnCompletion = "on-job-completion";

            public const string CleanupCalculationResultsForSpecificationProviders = "calculation-specification-provider-results-cleanup";

            public const string JobNotificationsToSignalR = "notifications-to-signalr";

            public const string CreateInstructAllocationsJob = "calculation-aggregations-job-completed";

            public const string UpdateDataDefinitionName = "data-definition-name-update";

            public const string UpdateCalculationFieldDefinitionProperties = "calculation-field-definition-properties-update";

            public const string UpdateScenarioFieldDefinitionProperties = "scenario-field-definition-properties-update";

            public const string ScaleUpCosmosdbCollection = "scale-up-cosmosdb-collection";
        }
    }
}
