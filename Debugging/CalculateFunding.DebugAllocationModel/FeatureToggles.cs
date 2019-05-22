using CalculateFunding.Common.FeatureToggles;

namespace CalculateFunding.DebugAllocationModel
{
    public class FeatureToggles : IFeatureToggle
    {
        public bool IsAggregateOverCalculationsEnabled()
        {
            return true;
        }

        public bool IsAggregateSupportInCalculationsEnabled()
        {
            return true;
        }

        public bool IsAllAllocationResultsVersionsInFeedIndexEnabled()
        {
            return true;
        }

        public bool IsAllocationLineMajorMinorVersioningEnabled()
        {
            return true;
        }

        public bool IsCalculationTimeoutEnabled()
        {
            return true;
        }

        public bool IsCheckJobStatusForChooseAndRefreshEnabled()
        {
            return true;
        }

        public bool IsDuplicateCalculationNameCheckEnabled()
        {
            return true;
        }

        public bool IsDynamicBuildProjectEnabled()
        {
            return true;
        }

        public bool IsExceptionMessagesEnabled()
        {
            return true;
        }

        public bool IsNewEditCalculationPageEnabled()
        {
            return true;
        }

        public bool IsNewManageDataSourcesPageEnabled()
        {
            return true;
        }

        public bool IsNewProviderCalculationResultsIndexEnabled()
        {
            return true;
        }

        public bool IsNotificationsEnabled()
        {
            return true;
        }

        public bool IsProcessDatasetDefinitionFieldChangesEnabled()
        {
            return true;
        }

        public bool IsProcessDatasetDefinitionNameChangesEnabled()
        {
            return true;
        }

        public bool IsProviderInformationViewInViewFundingPageEnabled()
        {
            return true;
        }

        public bool IsProviderProfilingServiceDisabled()
        {
            return true;
        }

        public bool IsProviderVariationsEnabled()
        {
            return true;
        }

        public bool IsPublishAndApprovePageFiltersEnabled()
        {
            return true;
        }

        public bool IsPublishButtonEnabled()
        {
            return true;
        }

        public bool IsRoleBasedAccessEnabled()
        {
            return true;
        }

        public bool IsSearchModeAllEnabled()
        {
            return true;
        }

        public bool IsUseFieldDefinitionIdsInSourceDatasetsEnabled()
        {
            return true;
        }
    }
}
