namespace CalculateFunding.Services.Profiling.Models
{
    public class DeliveryProfilePeriod : IProfilePeriod
    {
        public static DeliveryProfilePeriod CreateInstance(string period,
            int occurrence,
            PeriodType periodType,
            int periodYear,
            decimal profileValue,
            string distributionPeriod,
            int? calculationId = null)
        {
            return new DeliveryProfilePeriod(period, occurrence, periodType, periodYear, profileValue, distributionPeriod, calculationId);
        }

        public DeliveryProfilePeriod()
        {

        }

        private DeliveryProfilePeriod(string period, int occurrence, PeriodType periodType, int periodYear, decimal profileValue, string distributionPeriod, int? calculationId = null)
        {
            TypeValue = period;
            Occurrence = occurrence;
            Type = periodType;
            Year = periodYear;
            ProfileValue = profileValue;
            DistributionPeriod = distributionPeriod;
            CalculationId = calculationId;
        }

        public string TypeValue { get; set; }

        public int Occurrence { get; set; }

        public PeriodType Type { get; set; }

        public int Year { get; set; }

        public decimal ProfileValue { get; set; }

        public int? CalculationId { get; set; }

        public string DistributionPeriod { get; set; }
        
        public decimal GetProfileValue() => ProfileValue;

        public void SetProfiledValue(decimal value)
            => ProfileValue = value;
    }
}