using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Allocations.Models;
using Allocations.Models.Datasets;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Repository;
using Allocations.Services.Calculator;
using Allocations.Services.TestRunner;
using Allocations.Services.TestRunner.Vocab;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Allocations.Functions.Engine
{
    public static class OnTimerFired
    {
        [FunctionName("OnTimerFired")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log)
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
