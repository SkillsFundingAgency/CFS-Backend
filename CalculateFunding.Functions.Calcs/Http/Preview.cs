using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.Models;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.CSharp;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Calcs.Http
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
            var buildProject = (await budgetRepository.ReadAsync<BuildProject>(request.SpecificationId))?.Content;
            var calculation = buildProject?.Calculations.FirstOrDefault(x => x.Id == request.CalculationId);
            if (calculation == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

            if (!string.IsNullOrWhiteSpace(request.SourceCode))
            {
                calculation.Current.SourceCode  = request.SourceCode;
            }

            ISourceFileGenerator generator = null;
            switch (buildProject.TargetLanguage)
            {
                case TargetLanguage.CSharp:
                    generator = ServiceFactory.GetService<CSharpSourceFileGenerator>();
                    break;
                case TargetLanguage.VisualBasic:
                    generator = ServiceFactory.GetService<VisualBasicSourceFileGenerator>();
                    break;
            }

            var sourceFiles = generator.GenerateCode(buildProject);

            var compilerFactory = ServiceFactory.GetService<CompilerFactory>();

            var compiler = compilerFactory.GetCompiler(sourceFiles);


            var compilerOutput = compiler.GenerateCode(sourceFiles);


            var viewModel = new PreviewResponse()
            {
                Calculation = calculation,
                CompilerOutput = compilerOutput
            };


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