namespace CalculateFunding.Services.Core.Caching
{
    public static class CacheKeys
    {
        public const string TestScenarios = "test-scenarios:";

        public const string GherkinParseResult = "gherkin-parse-result:";

        public const string ProviderResultBatch = "provider-results-batch:";

        public const string ScopedProviderSummariesPrefix = "scoped-provider-summaries:";

        public const string FundingPeriods = "funding-periods";

        public const string FundingConfig = "funding-config:";

        public const string CalculationProgress = "calculation-progress:";

        public static string SpecificationSummaryById { get; set; } = "specification-summary:";

        public static string SpecificationCurrentVersionById { get; set; } = "specification-current-version:";

        public static string SpecificationSummariesByFundingPeriodId { get; set; } = "specification-summaries-funding-period:";

        public static string SpecificationSummaries { get; set; } = "specification-summaries";

        public static string CurrentCalculationsForSpecification { get; set; } = "calculations-current-for-specification:";

        public static string CalculationsSummariesForSpecification { get; set; } = "calculation-summaries-for-specification:";

        public static string CalculationsMetadataForSpecification { get; set; } = "calculations-metadata-for-specification:";

        public static string CurrentCalculation { get; set; } = "calculation-current:";
        public static string CalculationsForSpecification { get; set; } = "calculations-for-specification:";

        public static string AllProviderSummaries { get; set; } = "all-provider-summaries:";

        public static string AllProviderSummaryCount { get; set; } = " all-provider-summary-count:";

        public static string AllFundingStreams { get; set; } = "all-funding-streams";

        public static string UserById { get; set; } = "users";

        public static string DatasetRows { get; set; } = "ds-table-rows";

        public static string DatasetValidationStatus { get; set; } = "ds-validation-status";

        public static string AllocationLineResultStatusUpdates { get; set; } = "allocation-line-status-updates:";

        public static string AllCosmosScalingConfigs { get; set; } = "all-cosmos-scaling-configs";

        public static string FundingTemplatePrefix { get; set; } = "funding-template:";

        /// <summary>
        /// Effective Permissions - eg effective-permissions:userId
        /// </summary>
        public static string EffectivePermissions { get; set; } = "effective-permissions";

        public static string DatasetRelationshipFieldsForSpecification { get; set; } = "dataset-relationship-fields-for-specification:";

        public static string DatasetAggregationsForSpecification { get; set; } = "dataset-aggregations-for-specification:";

        public static string ApproveFundingForSpecification { get; set; } = "approved-funding-for-specification:";

        public static string JobDefinitions { get; set; } = "job-definitions";

        public static string CalculationAggregations { get; set; } = "calculation-aggregations:";

        public static string ProviderVersionMeta { get; set; } = "provider-version-metadata";

        public static string MasterProviderVersion { get; set; } = "master-version-provider";

        public static string ProviderVersion { get; set; } = "provider-version:";

        public static string ProviderVersionByDate { get; set; } = "provider-version-by-date:";

        public static string FundingTemplateContents { get; set; } = "funding-template-contents:";

        public static string FundingTemplateContentMetadata { get; set; } = "funding-template-content-metadata:";

        public static string TemplateMapping { get; set; } = "template-mapping:";
    }
}
