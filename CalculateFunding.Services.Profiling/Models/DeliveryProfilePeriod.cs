namespace CalculateFunding.Services.Profiling.Models
{
    public class DeliveryProfilePeriod
    {
        public DeliveryProfilePeriod()
        {

        }

        public DeliveryProfilePeriod(string period, int occurrence, PeriodType periodType, int periodYear, decimal profileValue, string distributionPeriod)
        {
            TypeValue = period;
            Occurrence = occurrence;
            Type = periodType;
            Year = periodYear;
            ProfileValue = profileValue;
            DistributionPeriod = distributionPeriod;
        }

        public DeliveryProfilePeriod(string distributionPeriod)
        {

            DistributionPeriod = distributionPeriod;
        }

        public string TypeValue { get; set; }

        public int Occurrence { get; set; }

        public PeriodType Type { get; set; }

        public int Year { get; set; }

        public decimal ProfileValue { get; set; }

        public string DistributionPeriod { get; set; }
    }
}