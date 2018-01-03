using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.ServiceBus;
using CalculateFunding.Functions.Common;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.Calcs
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var messagePump = ServiceFactory.GetService<MessagePump>();

            await messagePump.ReceiveAsync("spec-events", "spec-events-calcs", async json =>
            {
                await OnSpecEvent.Run(json, log);
            });
           

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
