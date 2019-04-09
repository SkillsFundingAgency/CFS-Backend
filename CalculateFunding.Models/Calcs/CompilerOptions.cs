using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CompilerOptions : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get { return SpecificationId; }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("optionStrictEnabled")]
        public bool OptionStrictEnabled { get; set; } = true;
    }
}
