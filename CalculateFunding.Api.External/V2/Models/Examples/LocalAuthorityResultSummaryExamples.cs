using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class LocalAuthorityResultSummaryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            LocalAuthorityResultsSummary summary = JsonConvert.DeserializeObject<LocalAuthorityResultsSummary>(Properties.Resources.V1_Sample_LocalAuthority_Results);
			foreach (LocalAuthorityResultSummary localAuthorityResultSummary in summary.LocalAuthorities)
			{
				foreach (LocalAuthorityProviderResultSummary localAuthorityProviderResultSummary in localAuthorityResultSummary.Providers)
				{
					localAuthorityProviderResultSummary.Provider.ProviderVariation = ProviderVariationExample.GetProviderVariationExample();
				}
			}
            return summary;
        }
    }


}