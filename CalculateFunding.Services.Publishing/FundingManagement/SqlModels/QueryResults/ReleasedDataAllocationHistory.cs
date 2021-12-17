using System;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class ReleasedDataAllocationHistory
    {
        public string ProviderId { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public string ChannelName { get; set; }
        public string ChannelCode { get; set; }
        public string VariationReasonName { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime StatusChangedDate { get; set; }
        public decimal TotalFunding { get; set; }
    }
}
