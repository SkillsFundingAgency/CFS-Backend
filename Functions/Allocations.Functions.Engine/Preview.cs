using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Allocations.Functions.Engine.Models;
using Allocations.Models;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Allocations.Functions.Engine
{
    public static class Preview
    {

        [FunctionName("preview")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            using (var budgetRepository = new Repository<Budget>("specs"))
            {
                var json = await req.Content.ReadAsStringAsync();

                var request = JsonConvert.DeserializeObject<PreviewRequest>(json, SerializerSettings);
                var budget = await budgetRepository.ReadAsync(request.BudgetId);
                var product = GetProduct(request.ProductId, budget);
                if (product == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                if (!string.IsNullOrWhiteSpace(request.Calculation))
                {
                    product.Calculation = new ProductCalculation {SourceCode = request.Calculation};
                }

                var compilerOutput = BudgetCompiler.GenerateAssembly(budget);

                var viewModel = new PreviewResponse()
                {
                    Product = product,
                    CompilerOutput = compilerOutput
                };

                var calc = new CalculationEngine(compilerOutput);

                if (compilerOutput.Success)
                {
                    foreach (var testProvider in product.TestProviders ?? new Reference[0])
                    {
                        var typedDatasets = await calc.GetProviderDatasets(testProvider);


                        var providerResult = calc.CalculateProviderProducts(testProvider, typedDatasets);
                        var testResult = calc.RunProviderTests(testProvider, typedDatasets, providerResult);
                        viewModel.TestResults.Add(testResult);
                    }
                }


                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(viewModel,
                        SerializerSettings), System.Text.Encoding.UTF8, "application/json")
                };
            }
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };

        private static Product GetProduct(string id, Budget budget)
        {
            Product product = null;
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    foreach (var productFolder in allocationLine.ProductFolders)
                    {
                        product = productFolder.Products.FirstOrDefault(x => x.Id == id);
                    }
                }
            }
            return product;
        }


    }
}