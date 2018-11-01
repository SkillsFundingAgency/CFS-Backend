namespace CalculateFunding.Services.Core.Constants
{
    public static class ServiceBusConstants
    {
        public const string ConnectionStringConfigurationKey = "ServiceBusSettings:ConnectionString";

        public static class QueueNames
        {
            public const string CalculationJobInitialiser = "calc-events-instruct-generate-allocations";

            public const string CalcEngineGenerateAllocationResults = "calc-events-generate-allocations-results";

            public const string TestEngineExecuteTests = "test-events-execute-tests";

            public const string AddDefinitionRelationshipToSpecification = "spec-events-add-definition-relationship";

            public const string ProcessDataset = "dataset-events-datasets";

            public const string ValidateDataset = "dataset-validate";

            public const string CreateDraftCalculation = "calc-events-create-draft";

            public const string UpdateBuildProjectRelationships = "calc-events-add-relationship-to-buildproject";

            public const string PublishProviderResults = "publish-provider-results";

            public const string FetchProviderProfile = "fetch-provider-profile";
        }

        public static class TopicNames
        {
            public const string EditSpecification = "edit-specification";

            public const string EditCalculation = "edit-calculation";
        }

        public static class TopicSubscribers
        {
            public const string UpdateCalculationsForEditSpecification = "calculation-update";

            public const string UpdateScenariosForEditSpecification = "test-scenario-update";

            public const string UpdateScenarioResultsForEditSpecification = "test-scenario-result-update";

            public const string UpdateUsersForEditSpecification = "users-update";

            public const string UpdateScenariosForEditCalculation = "test-scenario-update";

            public const string UpdateCalculationsForEditCalculation = "calcs-calculation-update";
        }
    }
}
