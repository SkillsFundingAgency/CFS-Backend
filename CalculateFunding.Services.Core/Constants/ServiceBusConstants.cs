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

            public const string ProcessDatasetPoisoned = "dataset-events-datasets/$DeadLetterQueue";

            public const string ProcessDatasetPoisonedLocal = "dataset-events-datasets-poisoned";

            public const string ValidateDataset = "dataset-validate";

            public const string ValidateDatasetPoisoned = "dataset-validate/$DeadLetterQueue";

            public const string ValidateDatasetPoisonedLocal = "dataset-validate-poisoned";

            public const string CreateDraftCalculation = "calc-events-create-draft";

            public const string UpdateBuildProjectRelationships = "calc-events-add-relationship-to-buildproject";

            public const string PublishingApproveFunding = "publishing-approvefunding";

            public const string PublishingApproveFundingPoisoned = "publishing-approvefunding/$DeadLetterQueue";

            public const string PublishingApproveFundingPoisonedLocal = "publishing-approvefunding-poisoned";

            public const string PublishingRefreshFunding = "publishing-refreshfunding";

            public const string PublishingRefreshFundingPoisoned = "publishing-refreshfunding/$DeadLetterQueue";

            public const string PublishingRefreshFundingPoisonedLocal = "publishing-refreshfunding-poisoned";

            public const string PublishingPublishFunding = "publishing-publishfunding";

            public const string PublishingPublishFundingPoisoned = "publishing-publishfunding/$DeadLetterQueue";

            public const string PublishingPublishFundingPoisonedLocal = "publishing-publishfunding-poisoned";

            public const string ReIndexCalculationResultsIndex = "reindex-calculation-results-index";

            public const string CalculationResultsCsvGeneration = "calculation-results-csv-generation";

            //For debug queue only as its on a timer
            public const string ScaleDownCosmosdbCollection = "scale-down-cosmosdb-collection";

            public const string IncrementalScaleDownCosmosdbCollection = "incremental-scale-down-cosmosdb-collection";

            public const string CalculationResultsCsvGenerationTimer = "calculation-results-csv-generation-timer";
            
            public const string PublishingReIndexPublishedProviders = "publishing-reindex-published-providers";
            
            public const string PublishingReIndexPublishedProvidersPoisoned = "publishing-reindex-published-providers/$DeadLetterQueue";
            
            public const string PublishingReIndexPublishedProvidersPoisonedLocal = "publishing-reindex-published-providers-poisoned";

            public const string DeleteCalculations = "calculations-delete";

            public const string DeleteCalculationResults = "calculation-results-delete";

            public const string DeleteDatasets = "datasets-delete";

            public const string DeleteTestResults = "test-results-delete";

            public const string DeleteTests = "tests-delete";

            public const string DeleteJobs = "jobs-delete";

            public const string DeleteSpecifications = "specifications-delete";
            
            public const string DeletePublishedProviders = "publishing-delete-published-providers";
        }

        public static class TopicNames
        {
            public const string EditSpecification = "edit-specification";

            public const string EditCalculation = "edit-calculation";

            public const string JobNotifications = "job-notifications";

            public const string DataDefinitionChanges = "data-definition-changes";

            public const string ProviderSourceDatasetCleanup = "provider-sourcedataset-cleanup";
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
