using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SpecificationFundingStatus
    {
        AlreadyChosen,
        SharesAlreadyChosenFundingStream,
        CanChoose
    }
}
