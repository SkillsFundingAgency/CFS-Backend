using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Models
{
    public class FundingChannelVersion
    {
        public string FundingId { get; set; }
        public int FundingMajorVersion { get; set; }
        public int GroupChannelVersion { get; set; }
        public string GroupingReason { get; set; }
        public string GroupingCode { get; set; }
        public string GroupingName { get; set; }
        public string GroupingTypeIdentifier { get; set; }
        public string GroupingIdentifierValue { get; set; }
        public string GroupingTypeClassification { get; set; }
        public double GroupingTotalFunding { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Author { get; set; }
        public string ChannelCode { get; set; }
        public string ProviderFundingId { get; set; }
        public string ProviderId { get; set; }
        public int ProviderMajorVersion { get; set; }
        public double ProviderTotalFunding { get; set; }
        public int ProviderChannelVersion { get; set; }
        public int ChannelId { get; set; }
    }
}
