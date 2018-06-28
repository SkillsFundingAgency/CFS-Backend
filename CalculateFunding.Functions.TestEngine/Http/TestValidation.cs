using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.TestEngine.Http
{
    public static class TestValidation
    {
        //[FunctionName("validate-test")]
        //public static Task<IActionResult> RunValidateTest(
        //[HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        //{
        //    using (var scope = IocConfig.Build().CreateHttpScope(req))
        //    {
        //        IGherkinParserService svc = scope.ServiceProvider.GetService<IGherkinParserService>();

        //        return svc.ValidateGherkin(req);
        //    }
        //}
    }
}
