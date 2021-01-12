using CalculateFunding.Services.Profiling.Models;

namespace CalculateFunding.Services.Profiling.Services
{
    public interface IFundingValueProfiler
    {
        AllocationProfileResponse ProfileAllocation(
            ProfileRequestBase request,
            FundingStreamPeriodProfilePattern profilePattern,
            decimal fundingValue);
    }
}