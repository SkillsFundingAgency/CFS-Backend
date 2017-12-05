using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using System;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Bootstrapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Functions.Common;

namespace CalculateFunding.EndToEnd
{

    class Program
    {
        static void Main(string[] args)
        {

            var importer = ServiceFactory.GetService<DataImporterService>();
            Task.Run(async () =>
            {
                await GenerateBudgetModel();

                var budgetDefinition = SeedData.CreateGeneralAnnualGrant();

                //foreach (var file in Directory.GetFiles("SourceData"))
                //{
                //    using (var stream = new FileStream(file, FileMode.Open))
                //    {
                //        await importer.GetSourceDataAsync(Path.GetFileName(file), stream, budgetDefinition.Id);
                //    }
                //}
                var compiler = ServiceFactory.GetService<BudgetCompiler>();
                var compilerOutput = compiler.GenerateAssembly(budgetDefinition);

                if (compilerOutput.Success)
                {
                    var calc = ServiceFactory.GetService<CalculationEngine>();
                    await calc.GenerateAllocations(compilerOutput);
                }
                else
                {
                    foreach (var compilerMessage in compilerOutput.CompilerMessages)
                    {
                        Console.WriteLine(compilerMessage.Message);
                    }
                    Console.ReadKey();
                }

                // await StoreAggregates(budgetDefinition, new AllocationFactory(compilerOutput.Assembly));



            }

            // Do any async anything you need here without worry
        ).GetAwaiter().GetResult();
        }

        public static IConfigurationRoot Configuration { get; set; }

        private static async Task GenerateBudgetModel()
        {
            var repository = ServiceFactory.GetService<Repository<Budget>>();

            await repository.CreateAsync(SeedData.CreateGeneralAnnualGrant());
            
        }

    }

}
