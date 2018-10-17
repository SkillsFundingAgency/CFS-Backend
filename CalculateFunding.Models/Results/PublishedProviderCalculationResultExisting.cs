namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResultExisting
    {
        public string Id { get; set; }

        public string ProviderId { get; set; }

        public decimal? Value { get; set; }

        public int CalculationVersion { get; set; }

        public string CalculationSpecificationId { get; set; }
    }
}
