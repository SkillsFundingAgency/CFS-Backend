namespace CalculateFunding.Services.Core.Constants
{
    public static class ServiceBusConstants
    {
        public const string ConnectionStringConfigurationKey = "ServiceBusSettings:ConnectionString";

        public static class QueueNames
        {
            public const string CalculationJobInitialiser = "calc-events-instruct-generate-allocations";

            public const string CalculationJobInitialiserPoisoned = "calc-events-instruct-generate-allocations/$DeadLetterQueue";

            public const string CalculationJobInitialiserPoisonedLocal = "calc-events-instruct-generate-allocations-poison";

            public const string CalcEngineGenerateAllocationResults = "calc-events-generate-allocations-results";

            public const string CalcEngineGenerateAllocationResultsPoisoned = "calc-events-generate-allocations-results/$DeadLetterQueue";

            public const string CalcEngineGenerateAllocationResultsPoisonedLocal = "calc-events-generate-allocations-results-poison";

            public const string TestEngineExecuteTests = "test-events-execute-tests";

            public const string AddDefinitionRelationshipToSpecification = "spec-events-add-definition-relationship";

            public const string ProcessDataset = "dataset-events-datasets";

            public const string ValidateDataset = "dataset-validate";

            public const string CreateDraftCalculation = "calc-events-create-draft";

            public const string UpdateBuildProjectRelationships = "calc-events-add-relationship-to-buildproject";

            public const string PublishProviderResults = "publish-provider-results";

            public const string PublishProviderResultsPoisoned = "publish-provider-results/$DeadLetterQueue";

            public const string PublishProviderResultsPoisonedLocal = "publish-provider-results-poison";

            public const string PublishingApproveFunding = "publishing-approvefunding";

            public const string PublishingRefreshFunding = "publishing-refreshfunding";

            public const string PublishingPublishFunding = "publishing-publishfunding";

            public const string FetchProviderProfile = "fetch-provider-profile";

            public const string FetchProviderProfilePoisoned = "fetch-provider-profile/$DeadLetterQueue";

            public const string FetchProviderProfilePoisonedLocal = "fetch-provider-profile-poison";

            public const string MigrateResultVersions = "migrate-result-versions";

            public const string MigrateFeedIndexId = "migrate-feed-index-id";

            public const string AllocationLineResultStatusUpdates = "allocationline-result-status-updates";

            public const string AllocationLineResultStatusUpdatesPoisoned = "allocationline-result-status-updates/$DeadLetterQueue";

            public const string AllocationLineResultStatusUpdatesPoisonedLocal = "allocationline-result-status-updates-poison";

            public const string InstructAllocationLineResultStatusUpdates = "allocationline-instruct-result-status-updates";

            public const string InstructAllocationLineResultStatusUpdatesPoisoned = "allocationline-instruct-result-status-updates/$DeadLetterQueue";

            public const string InstructAllocationLineResultStatusUpdatesPoisonedLocal = "allocationline-instruct-result-status-updates-poison";

            public const string ReIndexAllocationNotificationFeedIndex = "reindex-allocation-notifcication-feed-index";

            public const string ReIndexCalculationResultsIndex = "reindex-calculation-results-index";

            //For debug queue only as its on a timer
            public const string ScaleDownCosmosdbCollection = "scale-down-cosmosdb-collection";

            public const string IncrementaScaleDownCosmosdbCollection = "incremental-scale-down-cosmosdb-collection";
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
