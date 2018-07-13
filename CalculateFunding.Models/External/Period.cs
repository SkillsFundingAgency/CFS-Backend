namespace CalculateFunding.Models.External
{
    public class Period
    {
        public Period()
        {
        }

        public Period(string periodType, string periodId, string startDate, string endDate)
        {
            PeriodType = periodType;
            PeriodId = periodId;
            StartDate = startDate;
            EndDate = endDate;
        }

        public string PeriodType { get; set; }

        public string PeriodId { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

    }
}