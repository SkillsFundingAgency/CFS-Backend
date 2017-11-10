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
            using (var budgetRepository = new Repository<Budget>("specs"))
            {
                var budgets = budgetRepository.Query().ToList().Select(x => new BudgetSummary
                {
                    Budget = new Reference(x.Id, x.Name),
                    FundingPolicies = x.FundingPolicies.Select(fp => new FundingPolicySummary
                {
                    FundingPolicy = new Reference(fp.Id, fp.Name),            
                }).ToArray()}).ToList();
                
                using (var resultsRepository = new Repository<ProviderResult>("results"))
                {
                    using (var testResultsRepository = new Repository<ProviderTestResult>("results"))
                    {
                        var providerResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null).SelectMany(x => x.ProductResults).GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgets)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {
                                if (providerResults.ContainsKey(fundingPolicy.FundingPolicy.Id))
                                {
                                    fundingPolicy.TotalAmount = providerResults[fundingPolicy.FundingPolicy.Id].Sum(x => x.Value.Value);
                                }
                            }

                            budgetSummary.TotalAmount = budgetSummary.FundingPolicies.Sum(x => x.TotalAmount);
                        }


                        var providerTestResults = testResultsRepository.Query().ToList().Where(x => x.ScenarioResults != null).SelectMany(x => x.ScenarioResults).GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgets)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {

                                if (providerTestResults.ContainsKey(fundingPolicy.FundingPolicy.Id))
                                {
                                    fundingPolicy.TestSummary = new TestSummary
                                    {
                                        Passed = providerTestResults[fundingPolicy.FundingPolicy.Id]
                                            .Count(x => x.TestResult == TestResult.Passed),
                                        Failed = providerTestResults[fundingPolicy.FundingPolicy.Id]
                                            .Count(x => x.TestResult == TestResult.Failed),
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
