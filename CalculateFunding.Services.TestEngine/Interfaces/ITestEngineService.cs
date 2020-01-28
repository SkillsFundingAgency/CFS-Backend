using CalculateFunding.Services.TestRunner.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestEngineService
    {
        Task RunTests(Message message);

        Task<IActionResult> RunTests(TestExecutionModel testExecutionModel);
    }
}
