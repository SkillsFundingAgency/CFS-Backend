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

            var entity = await repository.ReadAsync<Implementation>(command.Id);
            var impl = entity?.Content ?? new Implementation{Id = command.Content.Id, TargetLanguage = TargetLanguage.VisualBasic};
            impl.Name = command.Content.Name;
            impl.Calculations = impl.Calculations ?? new List<CalculationImplementation>();
            impl.DatasetDefinitions = new List<DatasetDefinition>();
            

            impl.Calculations.AddRange(command.Content.GetCalculations()
                .Where(x => impl.Calculations.All(existing => existing.CalculationSpecification.Id != x.Id)).Select(x => new CalculationImplementation
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = x.Name,
                CalculationSpecification = x,
                Implementation = new Reference(impl.Id, impl.Name),
                Specification = command.Content,
               
                
            }));

            if (JsonConvert.SerializeObject(impl) !=
                JsonConvert.SerializeObject(entity?.Content ?? new Implementation()))
            {
                log.LogInformation($"Changes detected for implementation:{impl.Id}");
                var implCommand = new ImplementationCommand
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Content = impl,
                    Method = "POST",
                    User = command.User
                };
                await repository.CreateAsync(impl);
                await repository.CreateAsync(implCommand);
                await messenger.SendAsync("calc-events", implCommand);
                log.LogInformation($"Updated implementation:{impl.Id}");

            }
            else
            {
                log.LogInformation($"No changes for {impl.Id}");
            }


        }

    }
}
