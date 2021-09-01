namespace CalculateFunding.Services.Core.Caching
{
    public static class CacheKeys
    {
        public const string GherkinParseResult = "gherkin-parse-result:";

        public const string ProviderResultBatch = "provider-results-batch:";

        public const string ScopedProviderSummariesPrefix = "scoped-provider-summaries:";

        public const string ScopedProviderSummariesFilesystemKeyPrefix = "scoped-provider-summaries-filesystemkey:";

        public const string FundingPeriods = "funding-periods";

        public const string FundingConfig = "funding-config:";

        public const string CalculationResults = "calculationresults:";

        public const string FundingLineStructureTimestamp = "funding-line-structure-timestamp:";

        public const string SpecificationSummaryById  = "specification-summary:";

        public const string SpecificationSummariesByFundingPeriodId  = "specification-summaries-funding-period:";

        public const string SpecificationSummaries  = "specification-summaries";

        public const string CurrentCalculationsForSpecification  = "calculations-current-for-specification:";

        public const string CalculationsSummariesForSpecification  = "calculation-summaries-for-specification:";

        public const string CalculationsMetadataForSpecification  = "calculations-metadata-for-specification:";

        public const string CurrentCalculation  = "calculation-current:";
        
        public const string CalculationsForSpecification  = "calculations-for-specification:";

        public const string ScopedProviderSummariesCount  = "scoped-provider-summaries-count:";

        public const string ScopedProviderProviderVersion = "scoped-provider-summaries-provider-version:";

        public const string AllFundingStreams  = "all-funding-streams";

        public const string UserById  = "users";

        public const string DatasetRows  = "ds-table-rows";

        public const string DatasetValidationStatus  = "ds-validation-status";

        public const string AllocationLineResultStatusUpdates  = "allocation-line-status-updates:";

        public const string AllCosmosScalingConfigs  = "all-cosmos-scaling-configs";

        public const string FundingTemplatePrefix  = "funding-template:";

        public const string DisableTrackLatest = "disable-track-latest";

        /// <summary>
        /// Effective Permissions - eg effective-permissions:userId
        /// </summary>
        public const string EffectivePermissions  = "effective-permissions";

        public const string DatasetRelationshipFieldsForSpecification  = "dataset-relationship-fields-for-specification:";

        public const string DatasetAggregationsForSpecification  = "dataset-aggregations-for-specification:";

        public const string ApproveFundingForSpecification  = "approved-funding-for-specification:";

        public const string JobDefinitions  = "job-definitions";

        public const string CalculationAggregations  = "calculation-aggregations:";

        public const string ProviderVersionMetadata  = "provider-version-metadata";

        public const string MasterProviderVersion  = "master-version-provider";

        public const string ProviderVersion  = "provider-version:";

        public const string ProviderVersionByDate  = "provider-version-by-date:";

        public const string FundingTemplateContents  = "funding-template-contents:";

        public const string FundingTemplateContentMetadata  = "funding-template-content-metadata:";
        
        public const string FundingTemplateContentMetadataDistinct = "funding-template-content-metadata-distinct:";

        public const string TemplateMapping  = "template-mapping:";

        public const string LatestJobs = "jobs-latest:";

        public const string LatestJobsByJobDefinitionIds = "jobs-latest-by-job-definition-ids:";

        public const string LatestJobByEntityId = "jobs-latest-by-entity-id:";

        public const string LatestSuccessfulJobs = "jobs-latest-successful:";

        public const string CalculationFundingLines = "calculation-funding-lines:";

        public const string CodeContext = "code-contexts:";

        public const string CircularDependencies = "circular-dependencies:";

        public const string PupilNumberTemplateCalculationIds = "pupil-number-template-calculation-ids:";
    }
}
