using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Engine
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var budgets = await GetBudgets();
            foreach (var budget in budgets)
            {

                var compilerOutput = BudgetCompiler.GenerateAssembly(budget);


                var calc = new CalculationEngine(compilerOutput);
                await calc.GenerateAllocations();
            }


        }


        private static async Task<List<Budget>> GetBudgets()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                return repository.Query().ToList();
            }
        }
    }
}
