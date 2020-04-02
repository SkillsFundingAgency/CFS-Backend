using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ReportType
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
        CalcResult
    }
}
