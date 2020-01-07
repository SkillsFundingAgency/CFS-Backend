using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Etl.Extensions
{
    public static class DiagnosticExtensions
    {
        public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
        {
            // exited task completion source
            TaskCompletionSource<bool> exited = new TaskCompletionSource<bool>();

            // timeout task completion source
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            process.EnableRaisingEvents = true;
            process.Exited += (object obj, EventArgs args) =>
            {
                exited.TrySetResult(false);
            };

            if (process.HasExited)
            {
                exited.TrySetResult(false);
            }

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task exitedTask = exited.Task;
                Task completedTask;
                using (cts.Token.Register(o => ((TaskCompletionSource<bool>)o).SetResult(false), tcs))
                {
                    cts.CancelAfter(timeout);
                    completedTask = await Task.WhenAny(tcs.Task, exitedTask);
                }

                bool result = false;
                if (completedTask == exitedTask)
                {
                    await exitedTask;
                    result = true;
                }
                else
                {
                    // timeout has occured so need to kill the process
                    process.Kill();
                }

                return result;
            }
        }
    }
}
