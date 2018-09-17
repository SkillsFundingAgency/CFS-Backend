using System;
using System.Collections.Generic;
using CalculateFunding.Models.External;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.V1.Models.Examples
{
    public class AllocationWithHistoryExamples : IExamplesProvider
    {
        public object GetExamples()
        {
            AllocationWithHistoryModel allocation = JsonConvert.DeserializeObject<AllocationWithHistoryModel>(Properties.Resources.V1_Sample_Allocation_With_History);

            return allocation;
        }
    }
}