using CalculateFunding.Bootstrapper;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Repository;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Repositories.Common.Sql;
using CalculateFunding.Functions.Providers;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Search;

namespace CalculateFunding.EndToEnd
{

    class Program
    {
        static void Main(string[] args)
        {

            var importer = ServiceFactory.GetService<DataImporterService>();
            Task.Run(async () =>
            {
                //await GenerateBudgetModel();

                var budgetDefinition = SeedData.CreateGeneralAnnualGrant();

                var files = Directory.GetFiles("SourceData");
                foreach (var file in files.Where(x => x.ToLowerInvariant().EndsWith(".csv")))
                {
                    using (var blob = new FileStream(file, FileMode.Open))
                    {
                        try
                        {

                            var searchRepository = ServiceFactory.GetService<SearchRepository<ProviderIndex>>();
                            var stopWatch = new Stopwatch();
                            stopWatch.Start();

                            using (var reader = new StreamReader(blob))
                            {
                                var providers = new EdubaseImporterService().ImportEdubaseCsv(file, reader);

                                var dbContext = ServiceFactory.GetService<ProvidersDbContext>();

                                var command = new ProviderCommandEntity();

                                var addResult = await dbContext.ProviderCommands.AddAsync(command);
                                await dbContext.SaveChangesAsync();
                                stopWatch.Stop();
                                Console.WriteLine($"Read {file} in {stopWatch.ElapsedMilliseconds}ms");
                                stopWatch.Restart();

                                var events = (await dbContext.Upsert(addResult.Entity.Id, providers)).ToList();

                                stopWatch.Stop();
                                Console.WriteLine($"Bulk Inserted with {events.Count} changes in {stopWatch.ElapsedMilliseconds}ms");

                                var results = await searchRepository.Index(events.Select(Mapper.Map<ProviderIndex>).ToList());
                            }

                            Console.WriteLine($"C# Blob trigger function Processed blob\n Name:{file} \n Size: {blob.Length} Bytes");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                }


                foreach (var file in Directory.GetFiles("SourceData").Where(x => x.ToLowerInvariant().EndsWith(".xlsx")))
                {
                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        await importer.GetSourceDataAsync(Path.GetFileName(file), stream, budgetDefinition.Id);
                    }
                }
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
