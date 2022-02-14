namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class LatestFundingGroupVersion
    {
        public int FundingGroupVersionId { get; set; }

        public string FundingId { get; set; }

        public int MajorVersion { get; set; }
    }
}
