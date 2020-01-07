using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// A calculation used to build up a funding line.
    /// </summary>
    public class AggregateFundingCalculation
    {
        /// <summary>
        /// Create a calculation, setting properties to defaults. 
        /// </summary>
        public AggregateFundingCalculation()
        {
        }

        /// <summary>
        /// The template calculation id (i.e. a way to get to this property in the template).
        /// This value can be the same for multiple calculations within the hierarchy. 
        /// This indicates they will return the same value from the output.
        /// It allows input template to link calculations together, so a single calculation implemenation will be created instead of multiple depending on the hierarchy.
        /// 
        /// When templates are versioned, template IDs should be kept the same if they refer to the same thing, otherwise a new, unused ID should be used.
        /// </summary>
        [JsonProperty("templateCalculationId")]
        public uint TemplateCalculationId { get; set; }

        /// <summary>
        /// The value the calculation is resulting in.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Sub level calculations.
        /// </summary>
        [JsonProperty("calculations", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<AggregateFundingCalculation> Calculations { get; set; }
    }
}
