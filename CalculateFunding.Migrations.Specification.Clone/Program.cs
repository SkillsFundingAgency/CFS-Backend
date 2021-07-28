using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Clone.Clones;
using CommandLine;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<CloneOptions>(args)
                .MapResult(
                    RunEtl,
                    errs => Task.FromResult(1));
        }

        private static async Task RunEtl(CloneOptions options)
        {
            Console.WriteLine("Starting CloneSpec ...");

            Guard.ArgumentNotNull(options, "You must supply \"migrate\" settings to migrate a specification between two environments");

            IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider();
            ISpecificationClone specificationClone = (ISpecificationClone)serviceProvider.GetService(typeof(ISpecificationClone));

            await specificationClone.Run(options);

            Console.WriteLine("Completed CloneSpec.");

            Console.WriteLine();
            Console.WriteLine("Please press any key to stop this console application.");
            Console.ReadKey();
        }
    }
}
