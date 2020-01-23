namespace CalculateFunding.Services.Core.Constants
{
    public static class JobConstants
    {
        public static class DefinitionNames
        {
            public const string CreateAllocationJob = "CreateAllocationJob";

            public const string CreateInstructAllocationJob = "CreateInstructAllocationJob";

            public const string GenerateCalculationAggregationsJob = "GenerateCalculationAggregationsJob";

            public const string CreateInstructGenerateAggregationsAllocationJob = "CreateInstructGenerateAggregationsAllocationJob";

            public const string ValidateDatasetJob = "ValidateDatasetJob";

            public const string MapDatasetJob = "MapDatasetJob";

            public const string RefreshFundingJob = "RefreshFundingJob";

            public const string PublishProviderFundingJob = "PublishProviderFundingJob";

            public const string ApproveFunding = "ApproveFunding";

            public const string CreateSpecificationJob = nameof(CreateSpecificationJob);

            public const string AssignTemplateCalculationsJob = nameof(AssignTemplateCalculationsJob);

            public const string ReIndexPublishedProvidersJob = nameof(ReIndexPublishedProvidersJob);

            public const string DeleteSpecificationJob = nameof(DeleteSpecificationJob);
            public const string DeleteCalculationResultsJob = nameof(DeleteCalculationResultsJob);
            public const string DeleteCalculationsJob = nameof(DeleteCalculationsJob);
            public const string DeleteDatasetsJob = nameof(DeleteDatasetsJob);
            public const string DeleteJobsJob = nameof(DeleteJobsJob);
            public const string DeleteTestResultsJob = nameof(DeleteTestResultsJob);
            public const string DeleteTestsJob = nameof(DeleteTestsJob);

            public const string DeletePublishedProvidersJob = nameof(DeletePublishedProvidersJob);
        }

        public static class NotificationChannels
        {
            public const string All = "notifications";

            public const string SpecificationPrefix = "spec";

            public const string ParentJobs = "parentjobs";
        }

        public const string NotificationsHubName = "notifications";

        public const string NotificationsTargetFunction = "NotificationEvent";
    }
}
