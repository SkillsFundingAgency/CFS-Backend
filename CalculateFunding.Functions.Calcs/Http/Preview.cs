using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.Http
{
    public static class Preview
    {
        [FunctionName("compile-preview")]
        public static Task<IActionResult> RunCompliePreview(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IPreviewService svc = scope.ServiceProvider.GetService<IPreviewService>();

                return svc.Compile(req);
            }
        }
    }
}