using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using CalculateFunding.Api.External.V1.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Controllers
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