using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFundingService
    {
        Task<IActionResult> GetFundingByFundingResultId(string id);
    }
}
