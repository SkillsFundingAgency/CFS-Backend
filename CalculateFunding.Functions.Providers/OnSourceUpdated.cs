using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Services.DataImporter;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using ProviderCommand = CalculateFunding.Models.Providers.ProviderCommand;

namespace CalculateFunding.Functions.Providers
{
    public static class OnSourceUpdated
    {
        [FunctionName("OnSourceUpdated")]
        public static async Task RunAsync([BlobTrigger("edubase/{name}", Connection = "ProvidersStorage")]Stream blob, string name, TraceWriter log)
        {
 


            var importer = new EdubaseImporterService();
            using (var reader = new StreamReader(blob))
            {
                var providers = importer.ImportEdubaseCsv(name, reader);

                var dbContext = ServiceFactory.GetService<ProvidersDbContext>();

                var command = new Repositories.Providers.ProviderCommand { Id = Guid.NewGuid() };


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
                log.Info($"Bulk Insert in {stopWatch.ElapsedMilliseconds}ms");

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
                await dbContext.Database.ExecuteSqlCommandAsync(statement, new SqlParameter("@CommandId", command.Id));

                stopWatch.Stop();
                log.Info($"Merge in {stopWatch.ElapsedMilliseconds}ms");
            }

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        }
    }
}
