using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.Extensions
{
    public static class DeliveryProfilePeriodExtensions
    {
        public static DeliveryProfilePeriod WithValue(this DeliveryProfilePeriod original, decimal newValue)
        {
            return new DeliveryProfilePeriod(
                original.TypeValue,
                original.Occurrence,
                original.Type,
                original.Year,
                newValue,
                original.DistributionPeriod);
        }
    }
}