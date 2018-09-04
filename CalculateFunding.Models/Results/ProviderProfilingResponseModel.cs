using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{

    public class ProviderProfilingResponseModel
    {
        public ProviderProfilingResponseModel()
        {
            DeliveryProfilePeriods = Enumerable.Empty<ProfilingPeriod>();
        }

        public ProviderProfilingRequestModel AllocationProfileRequest { get; set; }

        public IEnumerable<ProfilingPeriod> DeliveryProfilePeriods { get; set; }
    }
}
