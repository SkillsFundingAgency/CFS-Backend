using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.Models;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
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

            var budgetRepository = ServiceFactory.GetService<CosmosRepository>();
            
            var json = await req.Content.ReadAsStringAsync();

            var request = JsonConvert.DeserializeObject<PreviewRequest>(json, SerializerSettings);
            var budget = await budgetRepository.ReadAsync<Implementation>(request.BudgetId);
            var product = budget?.Content?.Calculations.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

            if (!string.IsNullOrWhiteSpace(request.Calculation))
            {
                product.SourceCode  = request.Calculation;
            }

            var testSuite = new TestSuite();
            var calcTest = new CalculationTest();

            if (request.TestScenario != null)
            {
                // If we are given a scenario then remove everything else
                calcTest.TestScenarios = new List<TestScenario>{ request.TestScenario};
            }
            var generatorFactory = ServiceFactory.GetService<SourceFileGeneratorFactory>();


            var generator = generatorFactory.GetCompiler(budget.Content.TargetLanguage);

            var sourceFiles = generator.GenerateCode(budget.Content);

            var compilerFactory = ServiceFactory.GetService<CompilerFactory>();

            var compiler = compilerFactory.GetCompiler(sourceFiles);

            var compilerOutput = compiler.GenerateCode(sourceFiles);
                

            var viewModel = new PreviewResponse()
            {
                Product = product,
                CompilerOutput = compilerOutput
            };


            if (compilerOutput.Success)
            {
                var assembly = Assembly.Load(Convert.FromBase64String(compilerOutput.AssemblyBase64));
                var allocationFactory = new AllocationFactory(assembly);

                var calc = ServiceFactory.GetService<CalculationEngine>();
                var testEngine = ServiceFactory.GetService<TestEngine>();
                foreach (var testProvider in testSuite.TestProviders ?? new List<ProviderSummary>())
                {
                    var typedDatasets = await calc.GetProviderDatasets(allocationFactory, testProvider, request.BudgetId);


                    var providerResult = calc.CalculateProviderResults(allocationFactory.CreateAllocationModel(), budget.Content, testProvider, typedDatasets);
                    var testResult = testEngine.RunProviderTests(testSuite, providerResult);
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