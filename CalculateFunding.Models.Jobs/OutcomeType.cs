using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Jobs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OutcomeType
    {
        Succeeded,
        ValidationError,
        UserError,
        Inconclusive,
        Failed    
    }
}