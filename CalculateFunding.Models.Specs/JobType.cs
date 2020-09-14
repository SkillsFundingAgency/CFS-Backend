using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobType
    {
        Undefined,
        CurrentState,
        Released,
        History,
        HistoryProfileValues,
        CurrentProfileValues,
        CurrentOrganisationGroupValues,
        HistoryOrganisationGroupValues,
        HistoryPublishedProviderEstate,
        PublishedGroups,
        CalcResult
    }
}
