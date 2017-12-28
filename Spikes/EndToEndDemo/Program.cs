using System;
using System.IO;
using System.Threading.Tasks;
using Allocations.Boostrapper;
using Newtonsoft.Json;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using Newtonsoft.Json.Serialization;

namespace EndToEndDemo
{

    class Program
    {
        private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        static void Main(string[] args)
        {
            var importer = new DataImporterService(); 
            Task.Run(async () =>
            {
                await GenerateBudgetModel();




                var budgetDefinition = SeedData.CreateGeneralAnnualGrant();

                foreach (var file in Directory.GetFiles("SourceData"))
                {
                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        await importer.GetSourceDataAsync(Path.GetFileName(file), stream, budgetDefinition.Id);
                    }
                }

                var compilerOutput = BudgetCompiler.GenerateAssembly(budgetDefinition);

                if (compilerOutput.Success)
                {
                    var calc = new CalculationEngine(compilerOutput, TODO, TODO);
                    await calc.GenerateAllocations();
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
        ). GetAwaiter().GetResult();
    }


        private static async Task GenerateBudgetModel()
        {
            using (var repository = new Repository<Budget>("specs"))
            {
                await repository.CreateAsync(SeedData.CreateGeneralAnnualGrant());
            }
        }

    }

}
