using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V2.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams();

        Task<IActionResult> GetFundingStream(string fundingStreamId);
    }
}
