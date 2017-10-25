using System;
using System.ComponentModel.DataAnnotations;
using Allocations.Repository;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    [SearchIndex(IndexerForType = typeof(ProductTestScenarioResult), IndexerQuery = "SELECT m.id, m.customer.firstName, m.customer.lastName, m.customer.phoneNumber, m.customer.emailAddress, m.customer.altPhoneNumber, m.customer.altEmailAddress, m.customer.address.line1 AS addressLine1, m.customer.address.line2 AS addressLine2, m.customer.address.line3 AS addressLine3, m.customer.address.city, m.customer.address.state, m.customer.address.postcode, m.customer.address.country, m.customer.location, m.depot.id as depot, m.customer.language, m.customer.photoUrl, m.lastJourneyDate, m.nextJourneyDate, m.deleted, m._ts FROM m WHERE m.documentType = \"Membership\" AND m._ts > @HighWaterMark")]
    public class ProductTestScenarioResultIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("budget")]
        public string Budget { get; set; }
        [IsSearchable]
        [IsFacetable]
        [JsonProperty("fundingPolicy")]
        public string FundingPolicy { get; set; }
        [IsSearchable]
        [JsonProperty("hasPassed")]
        public string HasPassed { get; set; }
        [IsSearchable]
        [JsonProperty("productFolder")]
        public string ProductFolder { get; set; }
        [IsSearchable]
        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("lastFailedDate")]
        [IsFilterable]
        [IsFacetable]
        public DateTime? LastFailedDate { get; set; }


        [JsonProperty("lastPassedDate")]
        [IsFilterable]
        [IsFacetable]
        public DateTime? LastPassedDate { get; set; }

        //[JsonIgnore]
        //public string FullName => $"{FirstName} {LastName}";


    }
}