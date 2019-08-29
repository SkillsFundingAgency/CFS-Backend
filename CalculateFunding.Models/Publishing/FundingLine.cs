using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// A funding line.
    /// </summary>
    public class FundingLine
    {
        /// <summary>
        ///  Create a funding line, setting properties to defaults. 
        /// </summary>
        public FundingLine()
        {
        }

        /// <summary>
        /// The name of a funding line (e.g. "Total funding line").
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Funding Line Code - unique code within the template to lookup this specific funding line.
        /// Used to map this funding line in consuming systems (eg nav for payment)
        /// </summary>
        [JsonProperty("fundingLineCode", NullValueHandling = NullValueHandling.Ignore)]
        public string FundingLineCode { get; set; }

        /// <summary>
        /// The funding value in pence.
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// A unique ID (in terms of template, not data) for this funding line (e.g. 345).
        /// </summary>
        [JsonProperty("templateLineId")]
        public uint TemplateLineId { get; set; }

        /// <summary>
        /// The type of the funding line (e.g. paid on this basis, or informational only).
        /// </summary>
        [EnumDataType(typeof(OrganisationGroupingReason))]
        [JsonProperty("type")]
        public OrganisationGroupingReason Type { get; set; }

        /// <summary>
        /// Profile periods for this funding line
        /// </summary>
        [JsonProperty("distributionPeriods")]
        public IEnumerable<DistributionPeriod> DistributionPeriods { get; set; }
    }
}
