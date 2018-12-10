using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V2.Models.Examples
{
    public class FundingStreamExample : IExamplesProvider
    {
        public object GetExamples()
        {
            FundingStream fundingStream = JsonConvert.DeserializeObject<FundingStream>(Properties.Resources.V2_Sample_FundingStreams);

            return fundingStream;
        }
    }
}
