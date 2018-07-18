using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Api.External.ExampleProviders;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Models.External;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Controllers
{
    [Produces("application/vnd.sfa.allocation.1+json")]
    [Route("api/periods")]
    public class TimePeriodsController : Controller
    {
        /// <summary>
        /// Returns the time periods supported by the service
        /// </summary>
        /// <returns>A list of time periods </returns>
        [HttpGet]
        [Produces(typeof(List<Period>))]
        [SwaggerResponseExample(200, typeof(PeriodExamples))]
        [SwaggerOperation("getTimePeriods")]
        [SwaggerOperationFilter(typeof(OperationFilter<List<Period>>))]
        [ProducesResponseType(typeof(List<Period>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [SwaggerResponseHeader(200, "ETag", "string", "An ETag of the resource")]
        [SwaggerResponseHeader(200, "Cache-Control", "string", "Caching information for the resource")]
        [SwaggerResponseHeader(200, "Last-Modified", "date", "Date the resource was last modified")]

        public IActionResult Get()
        {
            return Ok(FakeData().ToArray());
        }

        internal static IEnumerable<Period> FakeData()
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{SwaggerConstants.StoreExportLocation}.periods.csv"))
            {
                using (var textReader = new StreamReader(stream))
                {
                    var csv = new CsvReader(textReader);
                    while (csv.Read())
                    {
                        var name = csv.GetField<string>(0);
                        var id = csv.GetField<string>(1);
                        int year = int.Parse(id.Substring(0, 4));
                        yield return new Period { PeriodId = id, PeriodType = "AY", StartDate = new DateTime(year, 9, 1), EndDate = new DateTime(year + 1, 8, 31) };

                    }
                }
            }
        }
    }
}
