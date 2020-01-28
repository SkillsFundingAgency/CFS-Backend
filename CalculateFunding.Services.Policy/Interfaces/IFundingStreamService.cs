using CalculateFunding.Models.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingStreamService
    {
        Task<IActionResult> GetFundingStreams();

        Task<IActionResult> GetFundingStreamById(string fundingStreamId);

        Task<IActionResult> SaveFundingStream(FundingStreamSaveModel fundingStreamSaveModel);
    }
}
