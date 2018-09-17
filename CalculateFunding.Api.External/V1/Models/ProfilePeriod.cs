using System;

namespace CalculateFunding.Api.External.V1.Models
{
    /// <summary>
    /// Represents a profiling period
    /// </summary>
    [Serializable]
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

        /// <summary>
        /// The period name
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// The occurrence of the period
        /// </summary>
        public int Occurrence { get; set; }

        /// <summary>
        /// The period year
        /// </summary>
        public string PeriodYear { get; set; }

        /// <summary>
        /// The period type
        /// </summary>
        public string PeriodType { get; set; }

        /// <summary>
        /// The period value
        /// </summary>
        public decimal ProfileValue { get; set; }

        /// <summary>
        /// The distribution period
        /// </summary>
        public string DistributionPeriod { get; set; }
    }
}