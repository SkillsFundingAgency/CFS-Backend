namespace CalculateFunding.FeatureToggles
{
    public interface IFeatureToggle
    {
        bool IsProviderProfilingServiceDisabled();

        bool IsAllocationLineMajorMinorVersioningEnabled();

        bool IsAggregateSupportInCalculationsEnabled();
    }
}
