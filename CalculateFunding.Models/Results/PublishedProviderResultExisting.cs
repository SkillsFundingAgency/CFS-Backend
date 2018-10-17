namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResultExisting
    {
        public string Id { get; set; }

        public AllocationLineStatus Status { get; set; }

        public string AllocationLineId { get; set; }

        public decimal? Value { get; set; }

        public string ProviderId { get; set; }

        public int Minor { get; set; }

        public int Major { get; set; }
    }
}
