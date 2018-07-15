using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Api.External.ExampleProviders;
using CalculateFunding.Api.External.Swagger.Helpers;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Models.External;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Controllers
{
    [ApiController]
    [Route("api/providers/{ukprn}/periods/{periodId}")]
    public class ProviderResultsController : Controller
    {
        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="includeUnpublished">By default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <returns>The funding streams totals for all funding streams relevant for the provider in the period specified</returns>
        [HttpGet]
        [Route("summary")]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [Produces(typeof(ProviderResultSummary))]
        [SwaggerResponseExample(200, typeof(ProviderResultSummaryExamples))]
        [SwaggerOperation("getProviderResultSummary")]
        [SwaggerOperationFilter(typeof(OperationFilter<ProviderResultSummary>))]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult Summary(string ukprn, string periodId, bool includeUnpublished = false)
        {
            var result = new ProviderResultSummary
            {
                Provider = new Provider(),
                Period = TimePeriodsController.FakeData().FirstOrDefault(x => x.PeriodId == periodId),
                FundingStreamResults = new List<FundingStreamResultSummary>()
            };

            var sbs = new PolicyResult { Policy = new Policy { PolicyId = "sbs", PolicyName = "School Block Share", PolicyDescription = "TBC" }, SubPolicyResults = new List<PolicyResult>() };

            var be = new PolicyResult { Policy = new Policy { PolicyId = "basic-entitlement", PolicyName = "Basic Entitlement", PolicyDescription = "TBC" }, Calculations = GetCalculationResults(ukprn, result.Provider, "sbs-basic-entitlement.csv") };
            be.TotalAmount = be.Calculations.Sum(x => x.CalculationAmount);
            var plf = new AllocationResult
            {
                AllocationLine = new AllocationLine { AllocationLineCode = "YPE13", AllocationLineName = "Pupil led factors" },
                AllocationAmount = be.Calculations.Sum(x => x.CalculationAmount)
            };
            sbs.SubPolicyResults.Add(be);
            sbs.TotalAmount = sbs.SubPolicyResults.Sum(x => x.TotalAmount);

            result.FundingStreamResults.Add(new FundingStreamResultSummary
            {
                FundingStream = new FundingStream
                {
                    FundingStreamCode = "YPLRE",
                    FundingStreamName = "Academies General Annual Grant"
                },
                Allocations = new List<AllocationResult> { plf },
                Policies = new List<PolicyResult> { sbs }
            });


            if (result?.Provider?.UKPRN == null) return NotFound();
            return Json(result);
        }

        /// <summary>
        /// Returns a summary of calculations for a given provider and policy in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="policyId">The required function stream's code</param>
        /// <param name="includeUnpublished">By default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <returns>The relevant calculations</returns>
        [HttpGet]
        [Route("policies/{policyId}")]
        [Produces(typeof(IEnumerable<ProviderPolicyResult>))]
        [SwaggerResponseExample(200, typeof(PolicyResultCalculationExamples))]
        [SwaggerOperation("getProviderPolicyResult")]
        [SwaggerOperationFilter(typeof(OperationFilter<ProviderPolicyResult>))]
        [ProducesResponseType(typeof(IEnumerable<ProviderPolicyResult>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult Policies(string ukprn, string periodId, string policyId, bool includeUnpublished = false)
        {
            var result = new ProviderPolicyResult
            {
                Provider = new Provider(),
                Period = TimePeriodsController.FakeData().FirstOrDefault(x => x.PeriodId == periodId),

            };

            var sbs = new PolicyResult { Policy = new Policy { PolicyId = "sbs", PolicyName = "School Block Share", PolicyDescription = "TBC" }, SubPolicyResults = new List<PolicyResult>() };

            var be = new PolicyResult { Policy = new Policy { PolicyId = "basic-entitlement", PolicyName = "Basic Entitlement", PolicyDescription = "TBC" }, Calculations = GetCalculationResults(ukprn, result.Provider, "sbs-basic-entitlement.csv") };
            be.TotalAmount = be.Calculations.Sum(x => x.CalculationAmount);
            var plf = new AllocationResult
            {
                AllocationLine = new AllocationLine { AllocationLineCode = "YPE13", AllocationLineName = "Pupil led factors" },
                AllocationAmount = be.Calculations.Sum(x => x.CalculationAmount)
            };
            sbs.SubPolicyResults.Add(be);
            sbs.TotalAmount = sbs.SubPolicyResults.Sum(x => x.TotalAmount);

            result.PolicyResult = sbs;


            if (result?.Provider?.UKPRN == null) return NotFound();
            return Json(result);
        }

        /// <summary>
        /// Returns a summary of calculations for a given provider and funding stream in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="periodId">The required period</param>
        /// <param name="fundingStreamCode">The required function stream's code</param>
        /// <param name="includeUnpublished">By default only published results are included. If includeUnpublished is true then the latest values are provided regardless of status</param>
        /// <returns>The relevant calculations</returns>
        [HttpGet]
        [Route("funding-streams/{fundingStreamCode}/{allocationLineCode}")]
        [Produces(typeof(IEnumerable<ProviderFundingStreamResult>))]
        [SwaggerResponseExample(200, typeof(FundingStreamResultCalculationExamples))]
        [SwaggerOperation("getProviderFundingStreamResult")]
        [SwaggerOperationFilter(typeof(OperationFilter<ProviderFundingStreamResult>))]
        [ProducesResponseType(typeof(IEnumerable<ProviderFundingStreamResult>), 200)]
        [ProducesResponseType(304)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(410)]
        [ProducesResponseType(415)]
        [ProducesResponseType(500)]
        public ActionResult AllocationsLineCodes(string ukprn, string periodId, string fundingStreamCode, string allocationLineCode, bool includeUnpublished = false)
        {
            var result = new ProviderFundingStreamResult
            {
                Provider = new Provider(),
                Period = TimePeriodsController.FakeData().FirstOrDefault(x => x.PeriodId == periodId),
                FundingStream = new FundingStream
                {
                    FundingStreamCode = "YPLRE",
                    FundingStreamName = "Academies General Annual Grant"
                },
                PolicyResults = new List<PolicyResult>()
            };

            var sbs = new PolicyResult { Policy = new Policy { PolicyId = "sbs", PolicyName = "School Block Share", PolicyDescription = "TBC" }, SubPolicyResults = new List<PolicyResult>() };

            var be = new PolicyResult { Policy = new Policy { PolicyId = "basic-entitlement", PolicyName = "Basic Entitlement", PolicyDescription = "TBC" }, Calculations = GetCalculationResults(ukprn, result.Provider, "sbs-basic-entitlement.csv") };
            be.TotalAmount = be.Calculations.Sum(x => x.CalculationAmount);
            sbs.SubPolicyResults.Add(be);
            sbs.TotalAmount = sbs.SubPolicyResults.Sum(x => x.TotalAmount);

            result.PolicyResults.Add(be);


            if (result?.Provider?.UKPRN == null) return NotFound();
            return Json(result);
        }

        private static string GetUPin(string ukPrn)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{SwaggerConstants.StoreExportLocation}.provider-allocations.csv"))
            {
                using (var textReader = new StreamReader(stream))
                {
                    var csv = new CsvReader(textReader);
                    while (csv.Read())
                    {
                        var allocationUkPrn = csv.GetField<string>(19);
                        if (allocationUkPrn == ukPrn)
                        {
                            return csv.GetField<string>(14);
                        }
                    }
                }
            }

            return null;
        }

        private static List<CalculationResult> GetCalculationResults(string ukPrn, Provider provider, string fileName)
        {
            List<CalculationResult> calculations = new List<CalculationResult>();

            var upin = GetUPin(ukPrn);
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{SwaggerConstants.StoreExportLocation}.{fileName}"))
            {
                using (var textReader = new StreamReader(stream))
                {
                    var csv = new CsvReader(textReader, new Configuration { Delimiter = "\t" });
                    csv.Read();
                    var i = 9;
                    var headers = new Dictionary<int, string>();
                    while (csv.TryGetField<string>(i, out var name))
                    {
                        headers.Add(i, name);
                        i++;
                    }

                    while (csv.Read())
                    {
                        var calcUpin = csv.GetField<string>(3);
                        if (upin == calcUpin)
                        {
                            if (provider.UKPRN == null)
                            {
                                provider.UKPRN = calcUpin;
                                provider.UPIN = csv.GetField<string>(4);
                                provider.LegalName = csv.GetField<string>(5);
                                //provider.ProviderOpenDate = csv.GetField<DateTime?>(20);
                            }

                            foreach (var header in headers)
                            {
                                calculations.Add(new CalculationResult
                                {
                                    CalculationStatus = "published",
                                    //SchemaVersion = 1.0M,
                                    CalculationVersionNumber = 3,
                                    CalculationAmount = csv.GetField<decimal>(header.Key),
                                    CalculationName = header.Value
                                });
                            }
                        }
                    }
                }
            }

            return calculations;
        }
    }
}
