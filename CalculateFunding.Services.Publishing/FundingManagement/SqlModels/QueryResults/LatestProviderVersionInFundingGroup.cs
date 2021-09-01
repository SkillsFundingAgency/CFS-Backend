namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class LatestProviderVersionInFundingGroup
    {
        public string ProviderId { get; set; }

        public int MajorVersion { get; set; }

        public string OrganisationGroupTypeCode { get; set; }

        public string OrganisationGroupIdentifierValue { get; set; }

        public string OrganisationGroupName { get; set; }

        public string OrganisationGroupTypeClassification { get; set; }

        public string GroupingReasonCode { get; set; }
    }
}
