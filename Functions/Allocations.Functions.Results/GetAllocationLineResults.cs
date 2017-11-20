using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using Allocations.Repository;
using Allocations.Models.Specs;
using Allocations.Models.Results;

namespace Allocations.Functions.Results
{
    public static class GetAllocationTestResults
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        [FunctionName("allocationLine")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            TraceWriter log)
        {
            string budgetId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "budgetId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            string allocationLineId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => String.Compare(q.Key, "allocationLineId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            if (budgetId == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "budgetId is required");
            }
            return await OnGet(budgetId, allocationLineId);
        }

        private static async Task<HttpResponseMessage> OnGet(string budgetId, string allocationLineId)
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                using (var resultsRepository = new Repository<ProviderResult>("results"))
                {
                    using (var testResultsRepository = new Repository<ProviderTestResult>("results"))
                    {
                        var allocationResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null)
                            .ToArray();
                        var resultsByAllocationLine = allocationResults.Select(x => new
                        {
                            ProductResults = x.ProductResults.Select(pr => new
                            {
                                BudgetId = x.Budget.Id,
                                ProviderId = x.Provider.Id,
                                AllocationLineId = pr.AllocationLine.Id,
                                FundingPolicyId = pr.FundingPolicy.Id,
                                Value = pr.Value ?? decimal.Zero
                            })
                        }).SelectMany(x => x.ProductResults).GroupBy(x => x.AllocationLineId).ToDictionary(x => x.Key);

                        var budget = await repository.ReadAsync(budgetId);
                        var allocationLine = budget?.GetAllocationLine(allocationLineId);
                        if (allocationLine == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                        foreach (var productFolder in allocationLine.ProductFolders)
                        {
                            foreach (var product in productFolder.Products)
                            {
                                if (resultsByAllocationLine.TryGetValue(allocationLine.Id,
                                    out var allocationLineResults))
                                {
                                    product.TotalProviders = allocationLineResults.GroupBy(x => x.ProviderId).Count();
                                    product.TotalAmount = allocationLineResults.Sum(x => x.Value);
                                }
                            }
                        }

                        var testsResults = testResultsRepository.Query().ToList();
                        var scenarioResults = testsResults.Where(sr => sr.ScenarioResults != null).Select(x => x)
                            .Select(x => new
                            {
                                ScenarioResults = x.ScenarioResults.Select(sr => new
                                {
                                    BudgetId = x.Budget.Id,
                                    ProviderId = x.Provider.Id,
                                    AllocationLineId = sr.AllocationLine.Id,
                                    FundingPolicyId = sr.FundingPolicy.Id,
                                    ProductId = sr.Product.Id,
                                    ScenarioName = sr.Scenario.Name,
                                    sr.TestResult,
                                    Covered = sr.TotalSteps == sr.StepExected
                                })
                            }).SelectMany(x => x.ScenarioResults).ToArray();

                        var resultsByProduct = scenarioResults.GroupBy(x => new { x.AllocationLineId, x.ProductId })
                            .ToDictionary(x => x.Key);

                        var resultsByScenario = scenarioResults.GroupBy(x => new { x.ScenarioName })
                            .ToDictionary(x => x.Key);

                        //foreach (var productFoldersa in allocationLine.ProductFolders)
                        //{
                        //    foreach (var product in productFoldersa.Products)
                        //    {
                        //        foreach (var scenario in product.TestScenarios)
                        //        {
                        //            if (resultsByScenario.TryGetValue(new
                        //            {
                        //                ProductId = product.Id,
                        //                ScenarioName = scenario.Name
                        //            }, out var scenarioTestResults))
                        //            {
                        //                var byScenarioAndProvider =
                        //                    scenarioTestResults.GroupBy(x => x.ProductId)
                        //                        .Select(x => new
                        //                        {
                        //                            Passed = x.All(tr => tr.TestResult == TestResult.Passed),
                        //                            Failed = x.Any(tr => tr.TestResult == TestResult.Failed),
                        //                            Ignored = x.All(tr => tr.TestResult == TestResult.Ignored),
                        //                        }).ToArray();
                        //                scenario.TestSummary = new TestSummary
                        //                {
                        //                    Passed = byScenarioAndProvider.Count(x => x.Passed),
                        //                    Failed = byScenarioAndProvider.Count(x => x.Failed)
                        //                };
                        //            }
                        //        }
                        //    }
                        //}

                        foreach (var productFolder in allocationLine.ProductFolders)
                        {
                            foreach (var product in productFolder.Products)
                            {

                                if (resultsByProduct.TryGetValue(new
                                {
                                    AllocationLineId = allocationLine.Id,
                                    ProductId = product.Id
                                }, out var allocationTestResults))
                                {
                                    var byProductAndProvider =
                                        allocationTestResults.GroupBy(x => x.ProviderId)
                                            .Select(x => new
                                            {
                                                Passed = x.All(tr => tr.TestResult == TestResult.Passed),
                                                Failed = x.Any(tr => tr.TestResult == TestResult.Failed),
                                                Ignored = x.All(tr => tr.TestResult == TestResult.Ignored),
                                            }).ToArray();
                                    product.TestSummary = new TestSummary
                                    {
                                        Passed = byProductAndProvider.Count(x => x.Passed),
                                        Failed = byProductAndProvider.Count(x => x.Failed)
                                    };
                                    if (product.TotalProviders > 0)
                                    {
                                        product.TestSummary.PassedRate =
                                            ((decimal)product.TestSummary.Passed / product.TotalProviders) * 100M;
                                        product.TestSummary.FailedRate =
                                            ((decimal)product.TestSummary.Failed / product.TotalProviders) * 100M;
                                        product.TestSummary.Coverage =
                                        ((decimal)(product.TestSummary.Passed + product.TestSummary.Failed) /
                                         product.TotalProviders) * 100M;
                                    }
                                }
                                else
                                {
                                    product.TestSummary = new TestSummary
                                    {
                                        Passed = 0,
                                        Failed = 0,
                                    };
                                }
                            }
                        }

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(allocationLine,
                                SerializerSettings), System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                }
            }
        }

        public static AllocationLine GetAllocationLine(this Budget budget, string allocationLineId)
        {
            AllocationLine allocationLine = null;
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                allocationLine = fundingPolicy.AllocationLines.FirstOrDefault(x => x.Id == allocationLineId);
                if (allocationLine != null) return allocationLine;
            }
            return null;
        }
    }
}
