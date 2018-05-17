using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationSummaryModel : Reference
    {
        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }
    }
}
