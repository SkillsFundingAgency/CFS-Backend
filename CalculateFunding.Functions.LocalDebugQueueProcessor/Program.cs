using CalculateFunding.Functions.Calcs;
using CalculateFunding.Functions.Results;
using CalculateFunding.Functions.Specs;
using System;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;

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
            IConfigurationRoot config = ConfigHelper.AddConfig();

            var hosts = new IShimHost[]
            {
                new ShimHost<CalcEventProcessor>(config, "dataset-events-results"),
                new ShimHost<DatasetProcessor>(config, "dataset-events-datasets"),
                new ShimHost<AddRelatioshipProcessor>(config, "spec-events-add-definition-relationship"),
                new ShimHost<CalcsCreateDraftEvent>(config, "calc-events-create-draft"),
                new ShimHost<CalcsGenerateAllocationsProcessor>(config, "calc-events-generate-allocations-results"),
                new ShimHost<CalcsInstructGenerationProcessor>(config, "calc-events-instruct-generate-allocations"),
                new ShimHost<CalcsAddRelationshipToBuildProjectProcessor>(config, "calc-events-add-relationship-to-buildproject")
            };

            foreach (var host in hosts)
            {
                await host.Register();
            }

            Console.WriteLine("Receiving. Press ENTER to stop worker.");
            Console.ReadLine();

            foreach (var host in hosts)
            {
                await host.Unregister();
            }
        }
    }
}
