using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class ProviderResultSummaryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            ProviderResultSummary providerResultSummary = JsonConvert.DeserializeObject<ProviderResultSummary>(Properties.Resources.V1_Sample_Provider_Results);

            return providerResultSummary;
        }
    }
}