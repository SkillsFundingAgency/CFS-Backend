using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V1.Interfaces
{
	public interface ITimePeriodsService
	{
		Task<IActionResult> GetFundingPeriods();
	}
}