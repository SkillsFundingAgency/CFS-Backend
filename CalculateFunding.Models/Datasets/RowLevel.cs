using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RowLevel
    {
        LocalAuthority,
        Provider,
        Learner,
        LearnerAims
    }
}