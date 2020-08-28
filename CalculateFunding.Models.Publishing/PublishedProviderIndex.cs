using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "publishedproviderindex")]
    public class PublishedProviderIndex
    {
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingStatus")]
        public string FundingStatus { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsFilterable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("ukprn")]
        public string UKPRN { get; set; }

        [IsFilterable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [IsFilterable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("urn")]
        public string URN { get; set; }

        [IsSortable, IsRetrievable(true)]
        [JsonProperty("fundingValue")]
        public double FundingValue { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("hasErrors")]
        public bool HasErrors { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("errors")]
        public string[] Errors { get; set; }
    }
}
