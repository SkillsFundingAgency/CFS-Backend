namespace CalculateFunding.Generators.NavFeed.Providers.v2
{
    public class Provider
    {
        public string ID { get; set; }
        public string Application { get; set; }
        public string Interface { get; set; }
        public string ImportStatus { get; set; }
        public string DateTimeCreated { get; set; }
        public string Preprocessed { get; set; }
        public string Processed { get; set; }
        public string Archived { get; set; }
        public string ErrorText { get; set; }
        public string NavisionReference { get; set; }
        public string ErrorLine { get; set; }
        public string DetailLines { get; set; }
        public string UnprocessedLines { get; set; }
        public string Boolean04 { get; set; }
        public string StartDay { get; set; }
        public string StartMonth { get; set; }
        public string EndDay { get; set; }
        public string EndMonth { get; set; }
        public string StartYear { get; set; }
        public string EndYear { get; set; }
        public string MajorVersionNo { get; set; }
        public string MinorVersionNo { get; set; }
        public string APIKey { get; set; }
        public string FundingStreamID { get; set; }
        public string FundingStreamName { get; set; }
        public string FundingShortName { get; set; }
        public string PeriodID  { get; set; }
        public string ProviderName { get; set; }
        public string ProviderLegalName { get; set; }
        public string UKPRN { get; set; }
        public string UPIN { get; set; }
        public string URN { get; set; }
        public string DFEEstablishNo { get; set; }
        public string EstablishmentNo { get; set; }
        public string LACode { get; set; }
        public string LocalAuthority { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string NAVVendorNo { get; set; }
        public string ProviderStatus { get; set; }
        public string AllocationID { get; set; }
        public string AllocationName { get; set; }
        public string AllocationShortName { get; set; }
        public string FundingRoute { get; set; }
        public string ContractRequired  { get; set; }
        public string AllocationStatus { get; set; }
        public string PeriodTypeID { get; set; }
        public string PeriodTypeName { get; set; }
        public string NAVVendorName { get; set; }
        public string Successors { get; set; }
        public string Precessors { get; set; }
        public string OpenReason { get; set; }
        public string CloseReason { get; set; }
        public string VariationReasons { get; set; }
        public string AllocationAmount { get; set; }

        public string OctoberDistributionPeriod
        {
            get
            {
                return "FY-1920";
            }
        }

        public string OctoberPeriod
        {
            get
            {
                return "October";
            }
        }

        public string OctoberPeriodYear
        {
            get
            {
                return "1920";
            }
        }

        public string OctoberOccurrence
        {
            get
            {
                return "1";
            }
        }

        public string OctoberPeriodType
        {
            get
            {
                return "CalendarMonth";
            }
        }

        public string AprilDistributionPeriod
        {
            get
            {
                return "FY-2021";
            }
        }

        public string AprilPeriod
        {
            get
            {
                return "April";
            }
        }

        public string AprilPeriodYear
        {
            get
            {
                return "1920";
            }
        }

        public string AprilOccurrence
        {
            get
            {
                return "0";
            }
        }

        public string AprilPeriodType
        {
            get
            {
                return "CalendarMonth";
            }
        }

        public string OctoberProfileValue { get; set; }
        public string AprilProfileValue { get; set; }
    }
}
