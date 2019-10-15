using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class ProfilingInMemoryClient : IProfilingApiClient
    {
        public async Task<ValidatedApiResponse<ProviderProfilingResponseModel>> GetProviderProfilePeriods(ProviderProfilingRequestModel requestModel)
        {
            return await Task.FromResult(new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "October", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "April", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 },
                DistributionPeriods = new List<Common.ApiClient.Profiling.Models.DistributionPeriods>
                 {
                    new Common.ApiClient.Profiling.Models.DistributionPeriods { DistributionPeriodCode = "2018-2019",   Value = 82190.0M }
                 }
            }));
        }

        public Task<NoValidatedContentApiResponse> SaveProfilingConfig(SetFundingStreamPeriodProfilePatternRequestModel requestModel)
        {
            throw new NotImplementedException();
        }
    }
}
