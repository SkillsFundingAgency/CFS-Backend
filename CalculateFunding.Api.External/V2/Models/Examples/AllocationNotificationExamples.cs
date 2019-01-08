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

            return feeds;
        }
    }
}
