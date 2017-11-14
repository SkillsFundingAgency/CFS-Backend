using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Allocations.Functions.Results.Models;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Repository;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Allocations.Models;

namespace Allocations.Functions.Results
{
    public static class GetBudgets
    {
        [FunctionName("budgets")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            using (var budgetRepository = new Repository<Allocations.Models.Specs.Budget>("specs"))
            {
                var test = budgetRepository.Query().ToList();
                var budgets = budgetRepository.Query().ToList().Select(x => new BudgetSummary
                {
                    Budget = new Reference(x.Id, x.Name),
                    FundingPolicies = x.FundingPolicies.Select(fp => new Models.FundingPolicy(fp.Id, fp.Name, fp.AllocationLines.Select(al => new Models.AllocationLine(al.Id, al.Name)).ToArray())).ToList().ToArray()
                }).ToArray();

                using (var resultsRepository = new Repository<ProviderResult>("results"))
                {
                    using (var testResultsRepository = new Repository<ProviderTestResult>("results"))
                    {
                        var asda = resultsRepository.Query().ToList();
                        var providerAllocationLineResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null).SelectMany(x => x.ProductResults).GroupBy(x => x.AllocationLine.Id).ToDictionary(x => x.Key);
                        var providerPolicyResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null).SelectMany(x => x.ProductResults).GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgets)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {
                                foreach (var allocationLine in fundingPolicy.AllocationLines)
                                {
                                    if (providerAllocationLineResults.ContainsKey(allocationLine.Id))
                                    {
                                        allocationLine.TotalAmount = providerAllocationLineResults[allocationLine.Id].Sum(x => x.Value.Value);
                                    }
                                }
                                //if (providerPolicyResults.ContainsKey(fundingPolicy.Id)) uncomment this for the project
                                //{
                                fundingPolicy.TotalAmount = fundingPolicy.AllocationLines.Sum(x => x.TotalAmount);
                                //}
                            }

                            budgetSummary.TotalAmount = budgetSummary.FundingPolicies.Sum(x => x.TotalAmount);
                        }

                        var providerTestResult1s = testResultsRepository.Query().ToList().Where(x => x.ScenarioResults != null).SelectMany(x => x.ScenarioResults).GroupBy(x => x.AllocationLine.Id).ToDictionary(x => x.Key);
                        var providerTestResults = testResultsRepository.Query().ToList().Where(x => x.ScenarioResults != null).SelectMany(x => x.ScenarioResults).GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgets)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {
                                foreach (var allocationLine in fundingPolicy.AllocationLines)
                                {
                                    if (providerTestResult1s.ContainsKey(allocationLine.Id))
                                    {
                                        allocationLine.TestSummary = new TestSummary
                                        {
                                            Passed = providerTestResult1s[allocationLine.Id]
                                           .Count(x => x.TestResult == TestResult.Passed),
                                            Failed = providerTestResult1s[allocationLine.Id]
                                           .Count(x => x.TestResult == TestResult.Failed),
                                        };
                                    }
                                    else
                                    {
                                        allocationLine.TestSummary = new TestSummary
                                        {
                                            Passed = 0,
                                            Failed = 0,
                                        };
                                    }
                                }
                                if (providerTestResults.ContainsKey(fundingPolicy.Id))
                                {
                                    fundingPolicy.TestSummary = new TestSummary
                                    {
                                        Passed = providerTestResults[fundingPolicy.Id]
                                            .Count(x => x.TestResult == TestResult.Passed),
                                        Failed = providerTestResults[fundingPolicy.Id]
                                            .Count(x => x.TestResult == TestResult.Failed),
                                    };
                                }
                                else
                                {
                                    fundingPolicy.TestSummary = new TestSummary
                                    {
                                        Passed = 0,
                                        Failed = 0,
                                    };
                                }
                                budgetSummary.TestSummary = new TestSummary
                                {
                                    Passed = budgetSummary.FundingPolicies.Where(x => x.TestSummary != null)
                                    .Sum(x => x.TestSummary.Passed),
                                    Failed = budgetSummary.FundingPolicies.Where(x => x.TestSummary != null)
                                    .Sum(x => x.TestSummary.Failed),
                                };
                            }
                        }

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(budgets,
                                new JsonSerializerSettings
                                {
                                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                    Formatting = Formatting.Indented
                                }), System.Text.Encoding.UTF8, "application/json")
                        };
                    }
                }
            }

        }

    }
}
