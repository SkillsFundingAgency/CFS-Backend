using CalculateFunding.Models.Scenarios;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IGherkinParserService
    {
        Task<IActionResult> ValidateGherkin(ValidateGherkinRequestModel validateGherkinRequestModel);
    }
}
