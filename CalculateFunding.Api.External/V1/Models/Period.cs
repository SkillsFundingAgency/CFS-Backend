using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class Period
    {
        public Period()
        {
        }

        public Period(string periodType, string periodId, DateTime startDate, DateTime endDate)
        {
            PeriodType = periodType;
            PeriodId = periodId;
            StartDate = startDate;
            EndDate = endDate;
        }

        /// <summary>
        /// The type of the period
        /// </summary>
        public string PeriodType { get; set; }

        /// <summary>
        /// The id of the period
        /// </summary>
        public string PeriodId { get; set; }

        /// <summary>
        /// The (inclusive) start date of the period
        /// </summary>
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// The (inclusive) end date of the period
        /// </summary>
        public DateTimeOffset EndDate { get; set; }

    }
}