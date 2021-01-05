using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public interface IReProfilingResponseMapper
    {
        IEnumerable<DistributionPeriod> MapReProfileResponseIntoDistributionPeriods(ReProfileResponse reProfileResponse);
    }
}