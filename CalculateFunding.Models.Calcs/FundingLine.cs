using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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
