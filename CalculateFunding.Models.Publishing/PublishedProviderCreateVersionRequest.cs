namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderCreateVersionRequest
    {
        public PublishedProvider PublishedProvider { get; set; }

        public PublishedProviderVersion NewVersion { get; set; }
    }
}
