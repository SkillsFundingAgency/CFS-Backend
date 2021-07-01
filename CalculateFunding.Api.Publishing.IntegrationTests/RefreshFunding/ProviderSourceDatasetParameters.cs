namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderSourceDatasetParameters
    {
        public string Id => $"{SpecificationId}_{DataRelationshipId}_{ProviderId}";
        public string ProviderId { get; set; }
        public string SpecificationId { get; set; }
        public string DataRelationshipId { get; set; }
        public string PartitionKey => ProviderId;
    }
}
