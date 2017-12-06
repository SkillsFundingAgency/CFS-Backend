using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using System;
using System.IO;
using System.Linq;
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
using CalculateFunding.Repositories.Providers;
using Microsoft.EntityFrameworkCore;

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
                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var providers = new EdubaseImporterService().ImportEdubaseCsv(Path.GetFileName(file), reader).ToList();

                            try
                            {
                                var dbContext = ServiceFactory.GetService<ProvidersDbContext>();

                                // var created = await dbContext.Database.EnsureCreatedAsync();
                                await dbContext.Database.MigrateAsync();

                                var command = new ProviderCommand {Id = Guid.NewGuid()};
                                await dbContext.ProviderCommands.AddAsync(command);
                                await dbContext.ProviderCommandCandidates.AddRangeAsync(providers.Take(100).Select(x =>
                                    new ProviderCommandCandidate
                                    {
                                        ProviderCommandId = command.Id,
                                        URN = x.URN,
                                        Name = x.Name
                                    }));
                                await dbContext.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
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
