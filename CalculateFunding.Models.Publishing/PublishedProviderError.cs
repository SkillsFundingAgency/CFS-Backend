namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderError
    {
        public string Identifier { get; set; }
        
        public PublishedProviderErrorType Type { get; set; }
        
        public string SummaryErrorMessage { get; set; }

        public string DetailedErrorMessage { get; set; }

        public string FundingLine { get; set; }

        public string FundingStreamId { get; set; }
    }
}