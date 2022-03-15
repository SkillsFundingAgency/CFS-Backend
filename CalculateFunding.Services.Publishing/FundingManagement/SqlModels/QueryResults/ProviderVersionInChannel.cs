using System;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    public class ProviderVersionInChannel
    {
        public Guid ReleasedProviderVersionChannelId { get; set; }

        public int ChannelId { get; set; }

        public string ChannelCode { get; set; }

        public string ChannelName { get; set; }

        public int MajorVersion { get; set; }

        public int MinorVersion { get; set; }

        public string ProviderId { get; set; }

        public string CoreProviderVersionId { get; set; }
    }
}
