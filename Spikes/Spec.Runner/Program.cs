using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Allocations.Specs;
using Xunit.Runners;

namespace Spec.Runner
{
    class Program
    {
        private static readonly object ConsoleLock = new object();
        private static readonly ManualResetEvent Finished = new ManualResetEvent(false);
        private static readonly int Result = 0;

        static int Main(string[] args)
        {
            using (var runner = AssemblyRunner.WithAppDomain(typeof(TestManualFeature).Assembly.Location))
            {
                
                runner.OnDiscoveryComplete += OnDiscoveryComplete;
                runner.OnExecutionComplete += OnExecutionComplete;
                runner.OnTestFailed += OnTestFailed;
                runner.OnTestPassed += OnTestPassed;
                runner.OnTestSkipped += OnTestSkipped;
                runner.OnDiagnosticMessage += OnDiagnosticMessage;
                runner.OnErrorMessage += OnErrorMessage;
                runner.Start();
                Finished.WaitOne();
                Finished.Dispose();

                return Result;
            }
        }

        private static void OnErrorMessage(ErrorMessageInfo errorMessageInfo)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{errorMessageInfo.ExceptionMessage} passed");
            }
        }

        private static void OnDiagnosticMessage(DiagnosticMessageInfo diagnosticMessageInfo)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{diagnosticMessageInfo.Message} passed");
            }
        }

        private static void OnTestSkipped(TestSkippedInfo testSkippedInfo)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{testSkippedInfo.TestDisplayName} passed");
            }
        }

        private static void OnTestPassed(TestPassedInfo testPassedInfo)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{testPassedInfo.TestDisplayName} passed");
            }
        }

        private static void OnTestFailed(TestFailedInfo testFailedInfo)
        {
            lock (ConsoleLock)
            {
                Console.WriteLine($"{testFailedInfo.TestDisplayName} passed");
            }
        }

        private static void OnExecutionComplete(ExecutionCompleteInfo executionCompleteInfo)
        {
            lock (ConsoleLock)
            {
                Finished.Set();
            }
        }

        private static void OnDiscoveryComplete(DiscoveryCompleteInfo discoveryCompleteInfo)
        {
            lock (ConsoleLock)
            {

            }
        }
    }
}
