using CalculateFunding.Common.Utility;
using CalculateFunding.Service.Core.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Helpers
{
    public class AzureStorageEmulatorAutomation : IDisposable
    {
        public bool StartedByAutomation { get; private set; }

        public void Dispose()
        {
            if (StartedByAutomation)
            {
                Task.WaitAll(Stop());
            }
        }

        public async Task Init()
        {
            if (!(await IsEmulatorRunning()))
            {
                await RunWithParameter("init /server \"(localdb)\\MsSqlLocalDb\"");
            }
        }

        public async Task Start()
        {
            if (!(await IsEmulatorRunning()))
            {
                await RunWithParameter("start");
                StartedByAutomation = true;
            }
        }

        public async Task Stop()
        {
            await RunWithParameter("stop");
        }

        public async Task ClearAll()
        {
            await RunWithParameter("clear all");
        }

        public async Task ClearBlobs()
        {
            await RunWithParameter("clear blob");
        }

        public async Task ClearTables()
        {
            await RunWithParameter("clear table");
        }

        public async Task ClearQueues()
        {
            await RunWithParameter("clear queue");
        }

        public static async Task<bool> IsEmulatorRunning()
        {
            var path = GetPathToStorageEmulatorExecutable();

            var process = new Process();

            process.StartInfo.FileName = path;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.Arguments = "status";
            process.Start();

            await process.WaitForExitAsync(TimeSpan.FromSeconds(10));

            StreamReader streamReader = process.StandardOutput;
            var output = streamReader.ReadToEnd();

            if (output.Contains("IsRunning: True"))
            {
                return true;
            }
            else if (output.Contains("IsRunning: False"))
            {
                return false;
            }

            process.Close();

            throw new ApplicationException("Unable to determine if Azure Storage Emulator is running.");
        }

        private static async Task RunWithParameter(string parameter)
        {
            Guard.IsNullOrWhiteSpace(parameter, nameof(parameter));

            var path = GetPathToStorageEmulatorExecutable();

            var process = new Process();

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = parameter;
            process.Start();

            await process.WaitForExitAsync(TimeSpan.FromSeconds(10));
        }

        private static string AzureSdkDirectory
        {
            get
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string path = Path.Combine(programFiles, @"Microsoft SDKs\Azure");
                return path;
            }
        }

        private static string GetPathToStorageEmulatorExecutable()
        {
            string[] paths = new string[]
            {
                Path.Combine(AzureSdkDirectory, @"Storage Emulator\AzureStorageEmulator.exe"),
                Path.Combine(AzureSdkDirectory, @"Storage Emulator\WAStorageEmulator.exe")
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException(
                "Unable to locate Azure storage emulator at any of the expected paths.",
                string.Join(", ", paths));
        }
    }
}
