using System;
using System.Threading.Tasks;

namespace CalculateFunding.Tests.Common.Helpers
{
    public static class Wait
    {
        public static async Task Until(Func<bool> condition,
            string timeOutMessage,
            int timeoutSeconds = 60,
            int retryDelayMilliseconds = 100)
        {
            int timeoutMilliseconds = timeoutSeconds * 1000;
            int startTime = Environment.TickCount;

            while (!condition())
            {
                if (Environment.TickCount - startTime >= timeoutMilliseconds)
                    throw new TimeoutException(timeOutMessage);

                await Task.Delay(retryDelayMilliseconds); 
            }
        }

        public static async Task Until(Func<Task<bool>> condition,
            string timeOutMessage,
            int timeoutSeconds = 60,
            int retryDelayMilliseconds = 100)
        {
            int timeoutMilliseconds = timeoutSeconds * 1000;
            int startTime = Environment.TickCount;

            while (!await condition())
            {
                if (Environment.TickCount - startTime >= timeoutMilliseconds)
                    throw new TimeoutException(timeOutMessage);

                await Task.Delay(retryDelayMilliseconds);
            }
        }
    }
}