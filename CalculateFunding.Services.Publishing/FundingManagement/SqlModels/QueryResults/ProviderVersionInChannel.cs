namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    public class ProviderVersionInChannel
    {
        public int ChannelId { get; set; }

        public int MajorVersion { get; set; }

        public string ProviderId { get; set; }
    }
}
