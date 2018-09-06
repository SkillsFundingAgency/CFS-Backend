using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class AllocationExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            AllocationModel allocation = JsonConvert.DeserializeObject<AllocationModel>(Properties.Resources.V1_Sample_Allocation);

            return allocation;
        }
    }
}