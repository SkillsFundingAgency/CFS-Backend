using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.TestRunner.Testing;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestEngineService : IProcessingService
    {
        Task<IActionResult> RunTests(TestExecutionModel testExecutionModel);
    }
}
