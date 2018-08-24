using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams(HttpRequest request);
    }
}
