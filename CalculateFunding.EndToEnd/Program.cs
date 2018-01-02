using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Functions.Specs.Http;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;

namespace CalculateFunding.EndToEnd
{

    class Program
    {
        static void Main(string[] args)
        {
            var budgetDefinition = new Implementation();
            var importer = ServiceFactory.GetService<DataImporterService>();
            Task.Run(async () =>
            {
                var specJson = File.ReadAllText(Path.Combine("SourceData", "spec.json"));

                ConsoleLogger logger = new ConsoleLogger("Default", (s, level) => true, true);
                await Specifications.RunCommands(GetHttpRequest(new SpecificationCommand
                {
                    Content =JsonConvert.DeserializeObject<Specification>(specJson),
                    Method = "POST",
                    Id = Guid.NewGuid().ToString("N"),
                    User = new Reference("matt.hammond@education.gov.uk", "Matt Hammond")
                }), logger);

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


                                stopWatch.Stop();
                                Console.WriteLine($"Read {file} in {stopWatch.ElapsedMilliseconds}ms");
                                stopWatch.Restart();

                                var list = providers.ToList();

                      //          var events = (await dbContext.Upsert(addResult.Entity.Id, list.Where(x => !string.IsNullOrEmpty(x.UKPRN)))).ToList();

                                stopWatch.Stop();
                            //    Console.WriteLine($"Bulk Inserted with {events.Count} changes in {stopWatch.ElapsedMilliseconds}ms");

                           //     var results = await searchRepository.Index(events.Select(Mapper.Map<ProviderIndex>).ToList());
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

        private static HttpRequest GetHttpRequest<T>(T payload) 
        {
            var httpRequest = new FakeHttpRequest{Method = "POST"};
            var json = JsonConvert.SerializeObject(payload);
            var buffer = Encoding.UTF8.GetBytes(json);
            var ms = new MemoryStream(buffer);
            ms.Seek(0, SeekOrigin.Begin);
            httpRequest.Body = ms;

            return httpRequest;
        }

        public static IConfigurationRoot Configuration { get; set; }


    }

    public class FakeHttpRequest : HttpRequest
    {
        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override HttpContext HttpContext { get; }
        public override string Method { get; set; }
        public override string Scheme { get; set; }
        public override bool IsHttps { get; set; }
        public override HostString Host { get; set; }
        public override PathString PathBase { get; set; }
        public override PathString Path { get; set; }
        public override QueryString QueryString { get; set; }
        public override IQueryCollection Query { get; set; }
        public override string Protocol { get; set; }
        public override IHeaderDictionary Headers { get; }
        public override IRequestCookieCollection Cookies { get; set; }
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }
        public override Stream Body { get; set; }
        public override bool HasFormContentType { get; }
        public override IFormCollection Form { get; set; }
    }
}
