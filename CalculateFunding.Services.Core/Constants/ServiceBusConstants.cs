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

            //public const string ProviderDatasetResults = "dataset-events-results";

            public const string ProcessDataset = "dataset-events-datasets";

            public const string CreateDraftCalculation = "calc-events-create-draft";

            public const string UpdateBuildProjectRelationships = "calc-events-add-relationship-to-buildproject";
        }
    }
}
