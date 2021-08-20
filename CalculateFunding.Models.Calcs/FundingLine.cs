using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class FundingLine
    {
        [JsonProperty("id")]
        public uint Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        [JsonProperty("sourceCodeName")]
        public string SourceCodeName { get; set; }

        public IEnumerable<FundingLineCalculation> Calculations { get; set; }

        public IEnumerable<FundingLine> FundingLines { get; set; }
    } 
}
