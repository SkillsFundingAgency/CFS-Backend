using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V1.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams();
    }
}
