using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Linq.Expressions;
using System;
using System.Linq;
using AutoMapper;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Specifications
    {
        [FunctionName("specifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestGetMethods<Specification>();
            return await restMethods.Run(req, log, "specificationId");
        }

        [FunctionName("specifications-by-year")]
        public static async Task<IActionResult> RunSpecificationsByYear(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestGetMethods<Specification>();

            req.Query.TryGetValue("academicYearId", out var yearId);

            var academicYearId = yearId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(academicYearId))
                return new BadRequestObjectResult("The required academic year id was not provided");

            Expression<Func<Specification, bool>> query = m => m.AcademicYear.Id == academicYearId;

            return await restMethods.Run(log, query);
        }

        [FunctionName("specifications-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            //req.HttpContext.RequestServices.GetService(typeof(IMappaer))
            //var mapper = ServiceFactory.GetService<IMapper>();

            var model = await req.ReadAsStringAsync();
            var restMethods = new RestCommandMethods<Specification, SpecificationCommand>("spec-events");
            return await restMethods.Run(req, log);
        }
    }
}
