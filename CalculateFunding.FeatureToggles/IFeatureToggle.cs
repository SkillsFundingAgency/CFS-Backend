namespace CalculateFunding.FeatureToggles
{
    public interface IFeatureToggle
    {
        bool IsProviderProfilingServiceEnabled();

        bool IsAllocationLineMajorMinorVersioningEnabled();

        bool IsAggregateSupportInCalculationsEnabled();
    }
}
