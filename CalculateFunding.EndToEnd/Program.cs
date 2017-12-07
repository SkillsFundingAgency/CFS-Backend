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

                                var command = new ProviderCommand {Id = Guid.NewGuid()};

                                   
                                await dbContext.ProviderCommands.AddAsync(command);
                                await dbContext.SaveChangesAsync();
                                var stopWatch = new Stopwatch();
                                stopWatch.Start();

                                await dbContext.BulkInsert("dbo.ProviderCommandCandidates", providers.Take(10000).Select(x => new ProviderCommandCandidate
                                    {
                                    ProviderCommandId = command.Id,
                                        CreatedAt = DateTimeOffset.Now,
                                        UpdatedAt = DateTimeOffset.Now,
                                        URN = x.URN,
                                        Name = x.Name,
                                        Address3 = x.Address3,
                                        Deleted = false
                                    }));

                                stopWatch.Stop();
                                Console.WriteLine($"Bulk Insert in {stopWatch.ElapsedMilliseconds}ms");
                                
                                stopWatch.Restart();
                                var merge = new MergeStatementGenerator
                                {
                                    CommandIdColumnName = "ProviderCommandId",
                                    KeyColumnName = "URN",
                                    ColumnNames = typeof(ProviderEntity).GetProperties().Select(x => x.Name.ToString()).ToList(),
                                    SourceTableName = "ProviderCommandCandidates",
                                    TargetTableName = "Providers"
                                };
                                var statement = merge.GetMergeStatement();
                                var name = new SqlParameter("@CommandId", command.Id);
                                await dbContext.Database.ExecuteSqlCommandAsync(statement, name);

                                stopWatch.Stop();
                                Console.WriteLine($"Merge in {stopWatch.ElapsedMilliseconds}ms");



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
