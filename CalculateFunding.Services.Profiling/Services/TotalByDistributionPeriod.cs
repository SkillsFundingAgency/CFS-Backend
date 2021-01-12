namespace CalculateFunding.Services.Profiling.Services
{
    public class TotalByDistributionPeriod
    {
        public TotalByDistributionPeriod(string distributionPeriodCode,
            decimal value)
        {
            DistributionPeriodCode = distributionPeriodCode;
            Value = value;
        }

        public string DistributionPeriodCode { get; }

        public decimal Value { get; }
    }
}