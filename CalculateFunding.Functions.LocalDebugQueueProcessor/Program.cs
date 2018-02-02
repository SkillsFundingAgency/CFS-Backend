using CalculateFunding.Functions.Calcs;
using CalculateFunding.Functions.Datasets.ServiceBus;
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
                await OnTimerFired.Run(null);
                await Task.Delay(3000);
            }
        }
    }
}
