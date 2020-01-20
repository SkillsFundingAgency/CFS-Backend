using CalculateFunding.Migrations.PublishedProviderPopulateReleased.Migrations;
using System;

namespace CalculateFunding.Migrations.PublishedProviderPopulateReleased
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting provider version migration ...");

                IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider();
                IPublishedProviderMigration publishedProviderMigration = (IPublishedProviderMigration)serviceProvider.GetService(typeof(IPublishedProviderMigration));

                await publishedProviderMigration.Run();

                Console.WriteLine("Completed published provider migration.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to complete published provider migration");
                Console.WriteLine(e);
            }

            Console.WriteLine();
            Console.WriteLine("Please press any key to stop this console application.");
            Console.ReadKey();
        }
    }
}
