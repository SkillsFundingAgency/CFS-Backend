using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApprovalMode
    {
        Undefined = 0,
        
        All = 1,
        
        Batches = 2
    }
}