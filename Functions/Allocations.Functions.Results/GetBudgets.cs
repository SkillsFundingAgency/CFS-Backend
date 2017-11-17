using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            using (var budgetRepository = new Repository<Budget>("specs"))
            {
                var budgets = budgetRepository.Query().ToList();
                var budgetSummaries = budgets.Select(x => new BudgetSummary
                {
                    Budget = new Reference(x.Id, x.Name),
                    FundingPolicies = x.FundingPolicies.Select(fp => new FundingPolicySummary(fp.Id, fp.Name, fp.AllocationLines?.Select(al => new AllocationLineSummary(al.Id, al.Name)).ToList())).ToList()
                }).ToArray();

                using (var resultsRepository = new Repository<ProviderResult>("results"))
                {
                    using (var testResultsRepository = new Repository<ProviderTestResult>("results"))
                    {
                        var allocationResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null).ToArray();
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
                        // var providerPolicyResults = resultsRepository.Query().ToList().Where(x => x.ProductResults != null).SelectMany(x => x.ProductResults).GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgetSummaries)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {
                                foreach (var allocationLine in fundingPolicy.AllocationLines ?? new List<AllocationLineSummary>())
                                {
                                    if (resultsByAllocationLine.TryGetValue(allocationLine.Id, out var allocationLineResults))
                                    {
                                        allocationLine.TotalProviders = allocationLineResults.GroupBy(x => x.ProviderId).Count();
                                        allocationLine.TotalAmount = allocationLineResults.Sum(x => x.Value);
                                    }
                                }
                                fundingPolicy.TotalProviders = fundingPolicy.AllocationLines?.Max(x => x.TotalProviders) ?? 0;
                                fundingPolicy.TotalAmount = fundingPolicy.AllocationLines?.Sum(x => x.TotalAmount) ?? decimal.Zero;

                            }
                            budgetSummary.TotalProviders = budgetSummary.FundingPolicies.Max(x => x.TotalProviders);
                            budgetSummary.TotalAmount = budgetSummary.FundingPolicies.Sum(x => x.TotalAmount);
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
                                    sr.TestResult,
                                    Covered = sr.TotalSteps == sr.StepExected
                                })
                            }).SelectMany(x => x.ScenarioResults).ToArray();

                        var resultsByAllocation = scenarioResults.GroupBy(x => new { x.BudgetId, x.AllocationLineId }).ToDictionary(x => x.Key);
                        var resultsByFundingPolicy = scenarioResults.GroupBy(x => new { x.BudgetId, x.FundingPolicyId }).ToDictionary(x => x.Key);
                        //var providerTestResults = scenarioResults.GroupBy(x => x.FundingPolicy.Id).ToDictionary(x => x.Key);
                        foreach (var budgetSummary in budgetSummaries)
                        {
                            foreach (var fundingPolicy in budgetSummary.FundingPolicies)
                            {
                                foreach (var allocationLine in fundingPolicy.AllocationLines ?? new List<AllocationLineSummary>())
                                {

                                    if (resultsByAllocation.TryGetValue(new
                                    {
                                        BudgetId = budgetSummary.Budget.Id,
                                        AllocationLineId = allocationLine.Id
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
                                        allocationLine.TestSummary = new TestSummary
                                        {
                                            Passed = byProductAndProvider.Count(x => x.Passed),
                                            Failed = byProductAndProvider.Count(x => x.Failed)
                                        };
                                        if (allocationLine.TotalProviders > 0)
                                        {
                                            allocationLine.TestSummary.PassedRate =
                                                ((decimal)allocationLine.TestSummary.Passed  / allocationLine.TotalProviders) * 100M;
                                            allocationLine.TestSummary.FailedRate =
                                                ((decimal)allocationLine.TestSummary.Failed / allocationLine.TotalProviders) * 100M;
                                            allocationLine.TestSummary.Coverage =
                                                ((decimal)(allocationLine.TestSummary.Passed + allocationLine.TestSummary.Failed) / allocationLine.TotalProviders) * 100M;
                                        }

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
                                if (resultsByFundingPolicy.TryGetValue(new
                                {
                                    BudgetId = budgetSummary.Budget.Id,
                                    FundingPolicyId = fundingPolicy.Id
                                }, out var fundingPolicyResults))
                                {
                                    var byProductAndProvider =
                                        fundingPolicyResults.GroupBy(x => new { x.ProviderId })
                                            .Select(x => new
                                            {
                                                Passed = x.All(tr => tr.TestResult == TestResult.Passed),
                                                Failed = x.Any(tr => tr.TestResult == TestResult.Failed),
                                                Ignored = x.All(tr => tr.TestResult == TestResult.Ignored),
                                            }).ToArray();
                                    fundingPolicy.TestSummary = new TestSummary
                                    {
                                        Passed = byProductAndProvider.Count(x => x.Passed),
                                        Failed = byProductAndProvider.Count(x => x.Failed)
                                    };
                                    if (fundingPolicy.TotalProviders > 0)
                                    {
                                        fundingPolicy.TestSummary.PassedRate =
                                            ((decimal)fundingPolicy.TestSummary.Passed / fundingPolicy.TotalProviders) * 100M;
                                        fundingPolicy.TestSummary.FailedRate =
                                            ((decimal)fundingPolicy.TestSummary.Failed / fundingPolicy.TotalProviders) * 100M;
                                        fundingPolicy.TestSummary.Coverage =
                                            ((decimal)(fundingPolicy.TestSummary.Passed + fundingPolicy.TestSummary.Failed) / fundingPolicy.TotalProviders) * 100M;
                                    }
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
                                if (budgetSummary.TotalProviders > 0)
                                {
                                    budgetSummary.TestSummary.PassedRate =
                                        ((decimal)budgetSummary.TestSummary.Passed / budgetSummary.TotalProviders) * 100M;
                                    budgetSummary.TestSummary.FailedRate =
                                        ((decimal)budgetSummary.TestSummary.Failed / budgetSummary.TotalProviders) * 100M;
                                    budgetSummary.TestSummary.Coverage =
                                        ((decimal)(budgetSummary.TestSummary.Passed + budgetSummary.TestSummary.Failed) / budgetSummary.TotalProviders) * 100M;
                                }
                            }
                        }

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(budgetSummaries,
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
