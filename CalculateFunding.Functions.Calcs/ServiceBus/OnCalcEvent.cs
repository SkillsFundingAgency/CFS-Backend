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
            impl.Calculations = impl.Calculations ?? new List<CalculationImplementation>();


            var compiler = ServiceFactory.GetService<BudgetCompiler>();
            impl.Build = compiler.GenerateAssembly(command.Content);

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
