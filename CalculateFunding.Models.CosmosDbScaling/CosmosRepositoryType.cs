using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.CosmosDbScaling
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CosmosCollectionType
    {
        [Description("calculationresults")]
        CalculationProviderResults,

        [Description("providerdatasets")]
        ProviderSourceDatasets,

        [Description("publishedfunding")]
        PublishedFunding,

        [Description("calcs")]
        Calculations,

        [Description("jobs")]
        Jobs,

        [Description("datasetaggregations")]
        DatasetAggregations,

        [Description("datasets")]
        Datasets,

        [Description("profiling")]
        Profiling,

        [Description("specs")]
        Specifications,

        [Description("testresults")]
        TestResults,

        [Description("tests")]
        Tests,

        [Description("users")]
        Users
    }
}
