using System;

namespace CalculateFunding.Models.External.V4
{
    public class ExternalFeedFundingGroupItem
    {
        public string FundingId { get; set; }

        public int MajorVersion { get; set; }

        public DateTime StatusChangedDate { get; set; }
    }
}
