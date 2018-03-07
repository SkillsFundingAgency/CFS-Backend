using CalculateFunding.Functions.Calcs;
using CalculateFunding.Functions.Datasets.ServiceBus;
using CalculateFunding.Functions.Results;
using CalculateFunding.Functions.Specs;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    static class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            while (true)
            {
                await OnCalcsCreateDraftTimerFired.Run(null);
                await OnCalcsGenerateAllocationResultsTimerFired.Run(null);
                await OnCalcsInstructAllocationResultsTimerFired.Run(null);
                await OnSpecsTimerFired.Run(null);
                await OnResultsTimerFired.Run(null);
                await Task.Delay(1);
            }
        }
    }
}
