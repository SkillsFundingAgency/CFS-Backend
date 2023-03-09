using System;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfilePeriodPattern
    {
        public ProfilePeriodPattern()
        {
        }

        public ProfilePeriodPattern(PeriodType periodType, 
            string period, 
            DateTime periodStartDate, 
            DateTime periodEndDate, 
            int periodYear, 
            int occurrence, 
            string distributionPeriod,
            decimal? periodPatternPercentage = null,
            int? periodPatternCalculationId = null)
        {
            PeriodType = periodType;
            Period = period;
            PeriodStartDate = periodStartDate;
            PeriodEndDate = periodEndDate;
            PeriodYear = periodYear;
            Occurrence = occurrence;
            DistributionPeriod = distributionPeriod;
            PeriodPatternPercentage = periodPatternPercentage;
            PeriodPatternCalculationId = periodPatternCalculationId;
        }

        [JsonProperty("periodType")]
        public PeriodType PeriodType { get; set; }

        [JsonProperty("period")]
        public string Period { get; set; }

        [JsonProperty("periodStartDate")]
        public DateTime PeriodStartDate { get; set; }

        [JsonProperty("periodEndDate")]
        public DateTime PeriodEndDate { get; set; }

        [JsonProperty("periodYear")]
        public int PeriodYear { get; set; }

        [JsonProperty("occurrence")]
        public int Occurrence { get; set; }

        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }

        [JsonProperty("periodPatternPercentage")]
        public decimal? PeriodPatternPercentage { get; set; }

        [JsonProperty("periodPatternCalculationId")]
        public int? PeriodPatternCalculationId { get; set; }
    }
}