using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsService
    {
        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> testResults);

    }
}
