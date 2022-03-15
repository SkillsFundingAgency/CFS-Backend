using System;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class LatestFundingGroupVersion
    {
        public Guid FundingGroupId { get; set; }

        public Guid FundingGroupVersionId { get; set; }

        public string FundingStreamCode { get; set; }

        public string FundingPeriodCode { get; set; }

        public string GroupingReasonCode { get; set; }

        public string OrganisationGroupTypeCode { get; set; }

        public string OrganisationGroupIdentifierValue { get; set; }

        public int MajorVersion { get; set; }
    }
}
