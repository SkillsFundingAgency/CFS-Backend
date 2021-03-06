﻿namespace CalculateFunding.Services.Core.Constants
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

            public const string MapScopedDatasetJobWithAggregation = "MapScopedDatasetJobWithAggregation";

            public const string MapScopedDatasetJob = "MapScopedDatasetJob";

            public const string MapFdzDatasetsJob = "MapFdzDatasetsJob";

            public const string RefreshFundingJob = "RefreshFundingJob";

            public const string PublishAllProviderFundingJob = nameof(PublishAllProviderFundingJob);

            public const string PublishBatchProviderFundingJob = nameof(PublishBatchProviderFundingJob);

            public const string ApproveAllProviderFundingJob = nameof(ApproveAllProviderFundingJob);

            public const string PublishIntegrityCheckJob = nameof(PublishIntegrityCheckJob);

            public const string ApproveBatchProviderFundingJob = nameof(ApproveBatchProviderFundingJob);

            public const string CreateSpecificationJob = nameof(CreateSpecificationJob);

            public const string AssignTemplateCalculationsJob = nameof(AssignTemplateCalculationsJob);

            public const string ProviderSnapshotDataLoadJob = nameof(ProviderSnapshotDataLoadJob);

            public const string ReIndexPublishedProvidersJob = nameof(ReIndexPublishedProvidersJob);

            public const string DeleteSpecificationJob = nameof(DeleteSpecificationJob);
            public const string DeleteCalculationResultsJob = nameof(DeleteCalculationResultsJob);
            public const string DeleteCalculationsJob = nameof(DeleteCalculationsJob);
            public const string DeleteDatasetsJob = nameof(DeleteDatasetsJob);
            public const string DeleteTestResultsJob = nameof(DeleteTestResultsJob);
            public const string DeleteTestsJob = nameof(DeleteTestsJob);

            public const string DeletePublishedProvidersJob = nameof(DeletePublishedProvidersJob);

            public const string ReIndexSpecificationCalculationRelationshipsJob = nameof(ReIndexSpecificationCalculationRelationshipsJob);

            public const string GenerateGraphAndInstructAllocationJob = nameof(GenerateGraphAndInstructAllocationJob);

            public const string GenerateGraphAndInstructGenerateAggregationAllocationJob = nameof(GenerateGraphAndInstructGenerateAggregationAllocationJob);

            public const string GeneratePublishedFundingCsvJob = nameof(GeneratePublishedFundingCsvJob);

            public const string GeneratePublishedProviderEstateCsvJob = nameof(GeneratePublishedProviderEstateCsvJob);

            public const string PopulateScopedProvidersJob = nameof(PopulateScopedProvidersJob);

            public const string PublishedFundingUndoJob = nameof(PublishedFundingUndoJob);
            
            public const string ReIndexTemplatesJob = nameof(ReIndexTemplatesJob);
            
            public const string ReIndexSpecificationJob = nameof(ReIndexSpecificationJob);

            public const string MergeSpecificationInformationForProviderJob = nameof(MergeSpecificationInformationForProviderJob);

            public const string UpdateCodeContextJob = nameof(UpdateCodeContextJob);

            public const string SearchIndexWriterJob = nameof(SearchIndexWriterJob);

            public const string ApproveAllCalculationsJob = nameof(ApproveAllCalculationsJob);

            public const string RunSqlImportJob = nameof(RunSqlImportJob);

            public const string GenerateCalcCsvResultsJob = nameof(GenerateCalcCsvResultsJob);
            
            public const string BatchPublishedProviderValidationJob = nameof(BatchPublishedProviderValidationJob);
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
