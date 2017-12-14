using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Providers;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.Scenarios
{
    public static class OnSourceUpdated
    {
        [FunctionName("OnSourceUpdated")]
        public static async Task RunAsync([BlobTrigger("edubase/{name}", Connection = "ProvidersStorage")]Stream blob, string name, TraceWriter log)
        {

            var searchRepository = ServiceFactory.GetService<SearchRepository<ProviderIndex>>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var importer = new EdubaseImporterService();
            using (var reader = new StreamReader(blob))
            {
                var providers = importer.ImportEdubaseCsv(name, reader);

                var dbContext = ServiceFactory.GetService<ProvidersDbContext>();

                var command = new ProviderCommandEntity();

                var addResult = await dbContext.ProviderCommands.AddAsync(command);
                await dbContext.SaveChangesAsync();
                stopWatch.Stop();
                log.Info($"Read {name} in {stopWatch.ElapsedMilliseconds}ms");
                stopWatch.Restart();

                var events = (await dbContext.Upsert(addResult.Entity.Id, providers.Where(x => !string.IsNullOrEmpty(x.UKPRN)))).ToList();

                stopWatch.Stop();
                log.Info($"Bulk Inserted with {events.Count} changes in {stopWatch.ElapsedMilliseconds}ms");

                var results = await searchRepository.Index(events.Select(Mapper.Map<ProviderIndex>).ToList());
            }

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        }
    }
}
