using System.Reflection;
using System.Text;
using CalculateFunding.Api.External.V1.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Controllers
{
    public class LocalAuthorityResultSummaryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            LocalAuthorityResultsSummary summary = JsonConvert.DeserializeObject<LocalAuthorityResultsSummary>(Properties.Resources.V1_Sample_LocalAuthority_Results);
            return summary;
        }
    }

  
}