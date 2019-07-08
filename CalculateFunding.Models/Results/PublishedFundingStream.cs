using System;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStream : IIdentifiable
    {
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{FundingStreamId}{ProviderId}{FundingPeriod.Id}"));
            }
        }

        [JsonProperty("fundingPeriod")]
        public Period FundingPeriod { get; set; }

        [JsonProperty("current")]
        public PublishedFundingStreamVersion Current { get; set; }
    }
}
