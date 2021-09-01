using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams();
        Task<IActionResult> GetFundingPeriods(string fundingStreamId);
        Task<IActionResult> GetFundingTemplateSourceFile(
            string fundingStreamId, string fundingPeriodId, int majorVersion, int minorVersion);
        Task<IActionResult> GetPublishedFundingTemplates(string fundingStreamId, string fundingPeriodId);
    }
}
