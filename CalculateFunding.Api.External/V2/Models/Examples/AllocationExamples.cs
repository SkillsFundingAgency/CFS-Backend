using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class AllocationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            AllocationModel allocation = JsonConvert.DeserializeObject<AllocationModel>(Properties.Resources.V2_Sample_Allocation);

            return allocation;
        }
    }
}