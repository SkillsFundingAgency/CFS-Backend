namespace CalculateFunding.Models.Providers.Requests
{
    public class SetFundingStreamCurrentProviderVersionRequest
    {
        public string FundingStreamId { get; set; }
        
        public string ProviderVersionId { get; set; }
    }
}