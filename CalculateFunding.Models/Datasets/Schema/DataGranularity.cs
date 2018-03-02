using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets.Schema
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataGranularity
    {
        Provider,
        Authority,
        Learner,
        LearnerAim,
        Other
    }

}