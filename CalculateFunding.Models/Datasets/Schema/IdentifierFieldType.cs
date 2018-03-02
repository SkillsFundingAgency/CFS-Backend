using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets.Schema
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentifierFieldType
    {
        UKPRN,
        UPIN,
        URN,
        EstablishmentNumber,
        Authority
    }

}