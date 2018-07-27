using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Models.Examples;
using CalculateFunding.Models.External;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace CalculateFunding.Api.External.V1.Controllers
{
    [Route("api/funding-streams")]
    public class FundingStreamController : Controller
    {
        /// <summary>
        /// Return the funding streams supported by the service
        /// </summary>
        /// <param name="ifNoneMatch">if a previously provided ETag value is provided, the service will return a 304 Not Modified response is the resource has not changed.</param>
        /// <param name="Accept">The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format.</param>
        /// <returns></returns>
        [HttpGet]
        [Produces(typeof(IEnumerable<FundingStream>))]
        [SwaggerResponseExample(200, typeof(FundingStreamExamples))]
        [SwaggerOperation("getFundingStreams")]
        [SwaggerOperationFilter(typeof(OperationFilter<List<FundingStream>>))]
        [ProducesResponseType(typeof(IEnumerable<FundingStream>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [SwaggerResponseHeader(200, "ETag", "string", "An ETag of the resource")]
        [SwaggerResponseHeader(200, "Cache-Control", "string", "Caching information for the resource")]
        [SwaggerResponseHeader(200, "Last-Modified", "date", "Date the resource was last modified")]

        public ActionResult GetFundingStreams()
        {
            return Ok(FakeData().ToList());
        }

        internal static IEnumerable<FundingStream> FakeData()
        {
            var allocationLines = FakeAllocationLines();
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CalculateFunding.Api.External.StoreExport.budgets.csv"))
            {
                using (var textReader = new StreamReader(stream))
                {
                    var csv = new CsvReader(textReader);
                    while (csv.Read())
                    {
                        var name = csv.GetField<string>(0);
                        var id = csv.GetField<string>(1);
                        allocationLines.TryGetValue(id, out var lines);
                        yield return new FundingStream
                        {
                            FundingStreamCode = id,
                            FundingStreamName = name,
                            AllocationLines = lines
                        };

                    }
                }
            }
        }

        private static Dictionary<string, List<AllocationLine>> FakeAllocationLines()
        {
            var results = new Dictionary<string, List<AllocationLine>>();
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("CalculateFunding.Api.External.StoreExport.allocation-lines.csv"))
            {
                using (var textReader = new StreamReader(stream))
                {
                    var csv = new CsvReader(textReader);
                    while (csv.Read())
                    {
                        var name = csv.GetField<string>(2);
                        var id = csv.GetField<string>(0);
                        var budgetId = csv.GetField<string>(1);
                        if (!results.TryGetValue(budgetId, out var allocationLines))
                        {
                            allocationLines = new List<AllocationLine>();
                            results.Add(budgetId, allocationLines);
                        }
                        allocationLines.Add(new AllocationLine
                        {
                            AllocationLineCode = id,
                            AllocationLineName = name
                        });

                    }
                }
            }
            return results;
        }
    }
}
