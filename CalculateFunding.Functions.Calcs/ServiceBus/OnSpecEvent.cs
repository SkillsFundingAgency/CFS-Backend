using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnSpecEvent
    {
        [FunctionName("on-spec-event")]
        public static async Task Run(
            [ServiceBusTrigger("spec-events", "spec-events-calcs", Connection = "ServiceBusConnectionString")]
            string messageJson,
            ILogger log)
        {
            log.LogInformation("spec-events-calcs triggered");
            var command = JsonConvert.DeserializeObject<SpecificationCommand>(messageJson);

            var repository = ServiceFactory.GetService<CosmosRepository>();
            var messenger = ServiceFactory.GetService<IMessenger>();

            var entity = repository.Query<BuildProject>().FirstOrDefault(x => x.Specification.Id == command.Content.Id);
            var impl = entity ?? new BuildProject{Id = command.Content.Id, TargetLanguage = TargetLanguage.VisualBasic};
            impl.Name = command.Content.Name;
            impl.Calculations = impl.Calculations ?? new List<Calculation>();
            impl.DatasetDefinitions = new List<DatasetDefinition>();


            //impl.Calculations.AddRange(command.Content.GenerateCalculations().Where(x =>
            //    impl.Calculations.All(existing => existing.CalculationSpecification.Id != x.Id)));

            if (JsonConvert.SerializeObject(impl) !=
                JsonConvert.SerializeObject(entity ?? new BuildProject()))
            {
                log.LogInformation($"Changes detected for implementation:{impl.Id}");
                //var implCommand = new ImplementationCommand
                //{
                //    Id = Reference.NewId(),
                //    Content = impl,
                //    Method = CommandMethod.Post,
                //    User = command.User
                //};
                await repository.CreateAsync(impl);
                //await repository.CreateAsync(implCommand);
                //await messenger.SendAsync("calc-events", implCommand);
                log.LogInformation($"Updated implementation:{impl.Id}");

            }
            else
            {
                log.LogInformation($"No changes for {impl.Id}");
            }


        }

    }
}
