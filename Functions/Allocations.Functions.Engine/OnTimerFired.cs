using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
using Allocations.Services.Compiler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace Allocations.Functions.Engine
{
    public static class OnTimerFired
    {
        [FunctionName("on-timer-fired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log, ExecutionContext context)
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
