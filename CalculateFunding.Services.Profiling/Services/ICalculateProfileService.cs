using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Services
{
	public interface ICalculateProfileService
	{
		Task<IActionResult> ProcessProfileAllocationRequest(ProfileRequest profileRequest);
		
		AllocationProfileResponse ProfileAllocation(
			ProfileRequestBase request, FundingStreamPeriodProfilePattern profilePattern, decimal fundingValue);

		Task<IActionResult> ProcessProfileAllocationBatchRequest(ProfileBatchRequest profileBatchRequest);
	}
}
