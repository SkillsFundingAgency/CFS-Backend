using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// A funding line.
    /// </summary>
    public class AggregateFundingLine
    {
        /// <summary>
        ///  Create a funding line, setting properties to defaults. 
        /// </summary>
        public AggregateFundingLine()
        {
        }

        /// <summary>
        /// The name of a funding line (e.g. "Total funding line").
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        public uint TemplateLineId { get; set; }

        public IEnumerable<AggregateFundingLine> FundingLines { get; set; }

        public IEnumerable<AggregateFundingCalculation> Calculations { get; set; }
    }
}
