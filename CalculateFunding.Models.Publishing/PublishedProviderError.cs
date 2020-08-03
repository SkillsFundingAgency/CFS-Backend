namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderError
    {
        public string Identifier { get; set; }
        
        public PublishedProviderErrorType Type { get; set; }
        
        public string Description { get; set; }
    }
}