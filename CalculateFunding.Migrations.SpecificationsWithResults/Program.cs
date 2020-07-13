using System;
using System.Threading.Tasks;
using CalculateFunding.Migrations.SpecificationsWithResults.Migrations;

namespace CalculateFunding.Migrations.SpecificationsWithResults
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting MergeSpecificationsWithProviderResultsDocuments ...");
                
                IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider();
                IMergeSpecificationsWithProviderResultsDocuments providerVersionMigration = (IMergeSpecificationsWithProviderResultsDocuments)serviceProvider.GetService(typeof(IMergeSpecificationsWithProviderResultsDocuments));

                await providerVersionMigration.Run();
                
                Console.WriteLine("Completed MergeSpecificationsWithProviderResultsDocuments."); 
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to complete MergeSpecificationsWithProviderResultsDocuments");
                Console.WriteLine(e);
            }
            
            Console.WriteLine();
            Console.WriteLine("Please press any key to stop this console application.");
            Console.ReadKey();
        }
    }
}