using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Interfaces
{
	public interface ITimePeriodsService
	{
		Task<IActionResult> GetFundingPeriods();
	}
}