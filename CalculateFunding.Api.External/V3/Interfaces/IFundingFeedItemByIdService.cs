using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFundingFeedItemByIdService
    {
        Task<IActionResult> GetFundingByFundingResultId(string id);
    }
}
