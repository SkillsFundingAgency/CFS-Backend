using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.Models;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Calcs
{
    public static class Preview
    {

        [FunctionName("preview")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {

            var budgetRepository = ServiceFactory.GetService<Repository<Budget>>();
            
            var json = await req.Content.ReadAsStringAsync();

            var request = JsonConvert.DeserializeObject<PreviewRequest>(json, SerializerSettings);
            var budget = await budgetRepository.ReadAsync(request.BudgetId);
            var product = budget?.Content.GetProduct(request.ProductId);
            if (product == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

            if (!string.IsNullOrWhiteSpace(request.Calculation))
            {
                product.Calculation = new CalculationImplementation { SourceCode = request.Calculation};
            }

            if (request.TestScenario != null)
            {
                // If we are given a scenario then remove everything else
                product.TestScenarios = new List<ProductTestScenario>{ request.TestScenario};
            }
            var compiler = ServiceFactory.GetService<BudgetCompiler>();
            var compilerOutput = compiler.GenerateAssembly(budget.Content);
                

            var viewModel = new PreviewResponse()
            {
                Product = product,
                CompilerOutput = compilerOutput
            };


            if (compilerOutput.Success)
            {
                var allocationFactory = new AllocationFactory(compilerOutput.Assembly);

                var calc = ServiceFactory.GetService<CalculationEngine>();

                foreach (var testProvider in product.TestProviders ?? new List<Reference>())
                {
                    var typedDatasets = await calc.GetProviderDatasets(allocationFactory, testProvider, request.BudgetId);


                    var providerResult = calc.CalculateProviderProducts(allocationFactory, compilerOutput, testProvider, typedDatasets);
                    var testResult = calc.RunProviderTests(compilerOutput, testProvider, typedDatasets, providerResult);
                    viewModel.TestResults.Add(testResult);
                }
            }


            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(viewModel,
                    SerializerSettings), System.Text.Encoding.UTF8, "application/json")
            };
            
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented

        };




    }
}