namespace CalculateFunding.Models.External
{
    public class ProfilePeriod
    {
        public ProfilePeriod()
        {
        }

        public ProfilePeriod(string period, int occurrence, string periodYear, string periodType, decimal profileValue,
            string distributionPeriod)
        {
            Period = period;
            Occurrence = occurrence;
            PeriodYear = periodYear;
            PeriodType = periodType;
            ProfileValue = profileValue;
            DistributionPeriod = distributionPeriod;
        }

        public string Period { get; set; }

        public int Occurrence { get; set; }

        public string PeriodYear { get; set; }

        public string PeriodType { get; set; }

        public decimal ProfileValue { get; set; }

        public string DistributionPeriod { get; set; }
    }
}