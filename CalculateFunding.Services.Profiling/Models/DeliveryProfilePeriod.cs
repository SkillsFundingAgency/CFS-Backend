namespace CalculateFunding.Services.Profiling.Models
{
    public class DeliveryProfilePeriod : IProfilePeriod
    {
        public static DeliveryProfilePeriod CreateInstance(string period,
            int occurrence,
            PeriodType periodType,
            int periodYear,
            decimal profileValue,
            string distributionPeriod)
        {
            return new DeliveryProfilePeriod(period, occurrence, periodType, periodYear, profileValue, distributionPeriod);
        }

        public DeliveryProfilePeriod()
        {

        }

        private DeliveryProfilePeriod(string period, int occurrence, PeriodType periodType, int periodYear, decimal profileValue, string distributionPeriod)
        {
            TypeValue = period;
            Occurrence = occurrence;
            Type = periodType;
            Year = periodYear;
            ProfileValue = profileValue;
            DistributionPeriod = distributionPeriod;
        }

        public string TypeValue { get; set; }

        public int Occurrence { get; set; }

        public PeriodType Type { get; set; }

        public int Year { get; set; }

        public decimal ProfileValue { get; set; }

        public string DistributionPeriod { get; set; }
        
        public decimal GetProfileValue() => ProfileValue;

        public void SetProfiledValue(decimal value)
            => ProfileValue = value;
    }
}