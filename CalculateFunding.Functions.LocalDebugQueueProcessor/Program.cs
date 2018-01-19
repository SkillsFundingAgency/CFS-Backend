using CalculateFunding.Functions.Calcs;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            while (true)
            {
                await OnTimerFired.Run(null, null);
                await Task.Delay(3000);
            }
        }
    }
}
