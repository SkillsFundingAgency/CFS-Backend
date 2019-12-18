namespace CalculateFunding.Services.Core.FeatureToggles
{
    public interface IFeatureToggle
    {
        bool IsProviderProfilingServiceDisabled();

        bool IsPublishButtonEnabled();

        bool IsPublishAndApprovePageFiltersEnabled();

        bool IsRoleBasedAccessEnabled();

        bool IsNewEditCalculationPageEnabled();

        bool IsNewManageDataSourcesPageEnabled();

        bool IsNewProviderCalculationResultsIndexEnabled();
        
        bool IsProviderResultsSpecificationCleanupEnabled();

        bool IsProviderInformationViewInViewFundingPageEnabled();

        bool IsDynamicBuildProjectEnabled();

        bool IsSearchModeAllEnabled();

        bool IsUseFieldDefinitionIdsInSourceDatasetsEnabled();

        bool IsProcessDatasetDefinitionNameChangesEnabled();

        bool IsProcessDatasetDefinitionFieldChangesEnabled();

        bool IsExceptionMessagesEnabled();

        bool IsCosmosDynamicScalingEnabled();
        
        bool IsDeletePublishedProviderForbidden();
    }
}