using CalculateFunding.Models.External.AtomItems;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class AllocationNotificationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            AtomFeed<AllocationModel> feeds = JsonConvert.DeserializeObject<AtomFeed<AllocationModel>>(Properties.Resources.V2_Sample_Allocation_Feeds);

            // Provider ID is XML ignored and json ignored, so provider ID does not get set from the value in the resources sample json
            foreach (var atomEntry in feeds.AtomEntry)
            {
                atomEntry.Content.Allocation.Provider.ProviderId = atomEntry.Content.Allocation.Provider.UkPrn;
				atomEntry.Content.Allocation.Provider.ProviderVariation = ProviderVariationExample.GetProviderVariationExample();
            }

            return feeds;
        }
    }
}
