using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Functions.Common;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Calcs
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            //var messagePump = ServiceFactory.GetService<MessagePump>();

            //await Task.WhenAll(
            //    messagePump.ReceiveAsync("spec-events", "spec-events-calcs", (Func<string, Task>)(async json => await OnSpecEvent.Run(json, log))),
            //    messagePump.ReceiveAsync("datasets-events", "dataset-events-calcs", (Func<string, Task>)(async json => await OnDatasetEvent.Run(json, log))),
            //    messagePump.ReceiveAsync("calc-events", "calc-events-calcs", (Func<string, Task>)(async json => await OnCalcEvent.Run(json, log)))
            //);
            using (var scope = IocConfig.Build().CreateScope())
            {
                var messagePump = scope.ServiceProvider.GetService<IMessagePumpService>();
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                await Task.WhenAll(
                   
                    messagePump.ReceiveAsync("calc-events", "calc-events-create-draft",
                        async message =>
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await calculationService.CreateCalculation(message);
                        },(exception) => {

                            logger.Error(exception, "An error occurred getting message from topic: calc-events for subscription: calc-events-create-draft");
                        } )

                        //messagePump.ReceiveAsync("spec-events", "spec-events-calcs", (Func<string, Task>)(async json => await OnSpecEvent.Run(json, log))),
                        //messagePump.ReceiveAsync("datasets-events", "dataset-events-calcs", (Func<string, Task>)(async json => await OnDatasetEvent.Run(json, log))),
                );

            }

            //  var dataset = new Repository<ProviderSourceDataset>();

            //var budgets = await GetBudgets();
            //foreach (var budget in budgets)
            //{

            //    var compilerOutput = BudgetCompiler.GenerateAssembly(budget);


            //    var calc = new CalculationEngine(, TODO, TODO);
            //    await calc.GenerateAllocations(compilerOutput);
            //}


        }


        //private static async Task<List<Budget>> GetBudgets()
        //{
        //    using (var repository = new Repository<Budget>("specs"))
        //    {
        //        return repository.Query().ToList();
        //    }
        //}
    }
}
