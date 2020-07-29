using System;
using System.Threading.Tasks;
using CalculateFunding.Migrations.DSG.RollBack.Migrations;
using CommandLine;

namespace CalculateFunding.Migrations.DSG.RollBack
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<MigrateOptions>(args)
                .MapResult(RunMigration,
                    errs => Task.FromResult(1));
        }

        private static async Task RunMigration(MigrateOptions options)
        {
            try
            {
                Console.WriteLine("Starting Dsg RollBack ...");

                IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider(options.CollectionName ?? "publishedfunding");
                IRollBackDsg migration = (IRollBackDsg) serviceProvider.GetService(typeof(IRollBackDsg));

                await migration.Run(options);

                Console.WriteLine("Completed Dsg RollBack ...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to run Dsg RollBack");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.WriteLine("Press any key to exit ...");
                Console.ReadKey();
            }
        }
    }
}