using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Results
{
    public static class GetAllocationLineResults
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        [FunctionName("allocationLine")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            TraceWriter log)
        {
            req.Query.TryGetValue("budgetId", out var budgetId);
            req.Query.TryGetValue("allocationLineId", out var allocationLineId);

            if (budgetId.FirstOrDefault() == null)
            {
                return new BadRequestErrorMessageResult("budgetId is required");
            }
            return await OnGet(budgetId, allocationLineId);
        }

        private static async Task<IActionResult> OnGet(string budgetId, string allocationLineId)
        {
            var repository = ServiceFactory.GetService<Repository<Budget>>();
            var resultsRepository = ServiceFactory.GetService<Repository<ProviderResult>>();
            var testResultsRepository = ServiceFactory.GetService<Repository<ProviderTestResult>>();


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
            var allocationLine = budget?.Content?.GetAllocationLine(allocationLineId);
            if (allocationLine == null) return new NotFoundResult();

            foreach (var productFolder in allocationLine.ProductFolders)
            {
                foreach (var product in productFolder.Products)
                {
                    if (resultsByAllocationLine.TryGetValue(product.Id,
                        out var allocationLineResults))
                    {
                        product.TotalProviders = allocationLineResults.GroupBy(x => x.ProviderId).Count();
                        product.TotalAmount = allocationLineResults.Sum(x => x.Value);
                    }
                }
                //productFolder.TotalProviders = productFolder.Products?.Max(x => x.TotalProviders) ?? 0;
                //productFolder.TotalAmount =
                //    productFolder.Products?.Sum(x => x.TotalAmount) ?? decimal.Zero;
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

            var resultsByProduct = scenarioResults.GroupBy(x => new { x.AllocationLineId, x.ProductId }).ToDictionary(x => x.Key);
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
                                ((decimal)product.TestSummary.Passed / product.TotalProviders) * 100M;
                            product.TestSummary.Coverage =
                                ((decimal)(product.TestSummary.Passed + product.TestSummary.Failed) / product.TotalProviders) * 100M;
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
            return new JsonResult(allocationLine);

        }

        public static AllocationLine GetAllocationLine(this Budget budget, string allocationLineId)
        {
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                var allocationLine = fundingPolicy.AllocationLines?.FirstOrDefault(x => x.Id == allocationLineId);
                if (allocationLine != null) return allocationLine;
            }
            return null;
        }
    }
}
