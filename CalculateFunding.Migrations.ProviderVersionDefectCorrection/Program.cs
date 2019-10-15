using System;
using System.Threading.Tasks;
using CalculateFunding.Migrations.ProviderVersionDefectCorrection.Migrations;

namespace CalculateFunding.Migrations.ProviderVersionDefectCorrection
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting provider version migration ...");
                
                IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider();
                IProviderVersionMigration providerVersionMigration = (IProviderVersionMigration)serviceProvider.GetService(typeof(IProviderVersionMigration));

                await providerVersionMigration.Run();
                
               Console.WriteLine("Completed provider version migration."); 
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to complete provider version migration");
                Console.WriteLine(e);
            }
            
            Console.WriteLine();
            Console.WriteLine("Please press any key to stop this console application.");
            Console.ReadKey();
        }
    }
}