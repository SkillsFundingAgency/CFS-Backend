using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class PeriodExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            Period[] fundingStreams = JsonConvert.DeserializeObject<Period[]>(Properties.Resources.V1_Sample_Periods);

            return fundingStreams;
        }
    }
}
