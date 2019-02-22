using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class AllocationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            AllocationModel allocation = JsonConvert.DeserializeObject<AllocationModel>(Properties.Resources.V2_Sample_Allocation);

            // Provider ID is XML ignored and json ignored, so provider ID does not get set from the value in the resources sample json
            allocation.Provider.ProviderId = allocation.Provider.UkPrn;

	        allocation.Provider.ProviderVariation = ProviderVariationExample.GetProviderVariationExample();

            return allocation;
        }
    }
}