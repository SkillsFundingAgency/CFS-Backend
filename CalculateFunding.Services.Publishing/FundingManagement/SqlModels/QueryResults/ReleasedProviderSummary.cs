﻿namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class ReleasedProviderSummary
    {
        public int ChannelId { get; set; }
        public string ProviderId { get; set; }
        public string FundingId { get; set; }
        public int ChannelVersion { get; set; }
        public string ChannelName { get; set; }
    }
}