using System;
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

                // var created = await dbContext.Database.EnsureCreatedAsync();
                await dbContext.Database.MigrateAsync();

                var command = new Repositories.Providers.ProviderCommand { Id = Guid.NewGuid() };
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

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        }
    }
}
