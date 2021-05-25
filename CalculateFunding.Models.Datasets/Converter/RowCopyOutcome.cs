using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets.Converter
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RowCopyOutcome
    {
        Unknown,
        Copied,
        ValidationFailure,
        DestinationRowAlreadyExists,
        SourceRowNotFound,
    }
}
