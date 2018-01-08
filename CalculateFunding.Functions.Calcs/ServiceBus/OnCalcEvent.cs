using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.CSharp;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnCalcEvent
    {
        [FunctionName("on-calc-event")]
        public static async Task Run(
            [ServiceBusTrigger("calc-events", "calc-events-calcs", Connection = "ServiceBusConnectionString")]
            string messageJson,
            ILogger log)
        {
            var command = JsonConvert.DeserializeObject<ImplementationCommand>(messageJson);

            var repository = ServiceFactory.GetService<CosmosRepository>();
            var messenger = ServiceFactory.GetService<IMessenger>();

            var entity = await repository.ReadAsync<Implementation>(command.Id);
            var impl = entity?.Content ?? new Implementation{Id = command.Content.Id};
            impl.Name = command.Content.Name;
            impl.Calculations = impl.Calculations ?? new List<Calculation>();


            ISourceFileGenerator generator = null;
            switch (impl.TargetLanguage)
            {
                case TargetLanguage.CSharp:
                    generator = ServiceFactory.GetService<CSharpSourceFileGenerator>();
                    break;
                case TargetLanguage.VisualBasic:
                    generator = ServiceFactory.GetService<VisualBasicSourceFileGenerator>();
                    break;
            }

            var sourceFiles = generator.GenerateCode(impl);

            var compilerFactory = ServiceFactory.GetService<CompilerFactory>();

            var compiler = compilerFactory.GetCompiler(sourceFiles);

            impl.Build = compiler.GenerateCode(sourceFiles);

            if (impl.Build.Success)
            {
                var calc = ServiceFactory.GetService<CalculationEngine>();
                var results = calc.GenerateAllocations(impl, new SpecificationScope());
            }
            else
            {
                foreach (var compilerMessage in impl.Build.CompilerMessages)
                {

                }

            }
        }

    }
}
