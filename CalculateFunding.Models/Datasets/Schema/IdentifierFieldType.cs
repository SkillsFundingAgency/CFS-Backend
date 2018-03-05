using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets.Schema
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentifierFieldType
    {
        None,
        UKPRN,
        UPIN,
        URN,
        EstablishmentNumber,
        Authority
    }

}