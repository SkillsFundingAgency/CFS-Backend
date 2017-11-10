using System;
using System.IO;
using System.Threading.Tasks;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
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

            var budgetDefinition = await GetBudget();

            var compilerOutput = BudgetCompiler.GenerateAssembly(budgetDefinition);


            var calc = new CalculationEngine(compilerOutput);
            await calc.GenerateAllocations();

        }


        private static async Task<Budget> GetBudget()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                return await repository.ReadAsync("budget-gag1718");
            }
        }
    }
}
