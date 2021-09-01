using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IFundingFeedItemByIdService
    {
        Task<IActionResult> GetFundingByFundingResultId(string channelId, string fundingId);
    }
}
