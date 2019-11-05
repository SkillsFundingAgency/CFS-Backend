using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy
{    
    public class FundingStreamSaveModel : Reference
    {
        [JsonProperty("shortName")]
        public string ShortName { get; set; }
    }
}
