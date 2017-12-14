using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Calcs
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

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
