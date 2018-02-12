namespace CalculateFunding.Services.Core.Helpers
{ 
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class RetryAgent
    {
        public static async Task DoAsync(Func<Task> func, int maxRetryAttempts = 3)
        {
            var currentRetryAttempt = 0;
            while (currentRetryAttempt < maxRetryAttempts)
            {
                try
                {
                    await func().ConfigureAwait(false);
                    return;
                }
                catch
                {
                    if (++currentRetryAttempt == maxRetryAttempts)
                    {
                        throw;
                    }
                }
            }
        }

        public static async Task<T> DoAsync<T>(Func<Task<T>> func, int maxRetryAttempts = 3, Func<T, bool, Task> test = null)
        {
            var currentRetryAttempt = 0;
            while (currentRetryAttempt < maxRetryAttempts)
            {
                try
                {
                    var result = await func().ConfigureAwait(false);
                    if (test != null)
                    {
                        await test.Invoke(result, currentRetryAttempt == (maxRetryAttempts - 1)).ConfigureAwait(false);
                    }

                    return result;
                }
                catch
                {
                    if (++currentRetryAttempt == maxRetryAttempts)
                    {
                        throw;
                    }
                }
            }

            throw new ApplicationException("The retry agent failed to correctly signal either success or failed.");
        }

        public static async Task<HttpResponseMessage> DoRequestAsync(Func<Task<HttpResponseMessage>> func, int maxRetryAttempts = 3, Func<HttpResponseMessage, bool, Task> test = null)
        {
            var currentRetryAttempt = 0;
            HttpResponseMessage result = null;
            while (currentRetryAttempt < maxRetryAttempts)
            {
                try
                {
                    result = await func().ConfigureAwait(false);
                    if (test != null)
                    {
                        await test.Invoke(result, currentRetryAttempt == (maxRetryAttempts - 1)).ConfigureAwait(false);
                    }

                    if ((int)result.StatusCode >= 500)
                    {
                        throw new Exception($"The request failed with status: {result.StatusCode.ToString()} with reason: {result.ReasonPhrase}");
                    }

                    return result;
                }
                catch
                {
                    if (++currentRetryAttempt == maxRetryAttempts)
                    {
                        return result;
                    }
                }
            }

            throw new ApplicationException("The retry agent failed to correctly signal either success or failed.");
        }

        public static T Do<T>(Func<T> func, int maxRetryAttempts = 3)
        {
            var currentRetryAttempt = 0;
            while (currentRetryAttempt < maxRetryAttempts)
            {
                try
                {
                    var result = func();
                    return result;
                }
                catch
                {
                    if (++currentRetryAttempt == maxRetryAttempts)
                    {
                        throw;
                    }
                }
            }

            return default(T);
        }

        public static void Do(Action action, int maxRetryAttempts = 3)
        {
            var currentRetryAttempt = 0;
            while (currentRetryAttempt < maxRetryAttempts)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (++currentRetryAttempt == maxRetryAttempts)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
