using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.DataImporter;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using CalculateFunding.Functions.Calcs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using CalculateFunding.Functions.Datasets.Http;
using CalculateFunding.Functions.Results.Http;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.CSharp;
using CalculateFunding.Services.CodeGeneration.VisualBasic;

namespace CalculateFunding.EndToEnd
{

    class Program
    {
        static void Main(string[] args)
        {
            var importer = ServiceFactory.GetService<DataImporterService>();
            ConsoleLogger logger = new ConsoleLogger("Default", (s, level) => true, true);

            var user = new Reference("matt.hammond@education.gov.uk", "Matt Hammond");
            Task.Run(async () =>
            {


                var spec = await ImportSpecification(user, logger);


                var scopeJson = File.ReadAllText(Path.Combine("SourceData", "scope.json"));

                var scope = JsonConvert.DeserializeObject<SpecificationScope>(scopeJson);


 
                var impl = new Implementation
                {
                    Id = Reference.NewId(),
                    Specification = spec.GetReference(),
                    TargetLanguage = TargetLanguage.VisualBasic,
                    Name = spec.Name
                };
                impl.Calculations = impl.Calculations ?? new List<Calculation>();
                impl.DatasetDefinitions = new List<DatasetDefinition>();



                impl.Calculations.AddRange(spec.GenerateCalculations());

                ISourceFileGenerator generator = null;
                switch (impl.TargetLanguage)
                {
                    case TargetLanguage.CSharp:
                        generator = ServiceFactory.GetService<CSharpSourceFileGenerator>();
                        break;
                    case TargetLanguage.VisualBasic:
                        generator = ServiceFactory.GetService<VisualBasicSourceFileGenerator>();
                        break;
                }

                var sourceFiles = generator.GenerateCode(impl);

                var compilerFactory = ServiceFactory.GetService<CompilerFactory>();

                var compiler = compilerFactory.GetCompiler(sourceFiles);


                impl.Build = compiler.GenerateCode(sourceFiles);
                foreach (var sourceFile in impl.Build.SourceFiles)
                {
                    File.WriteAllText($@"..\Spikes\{impl.TargetLanguage.ToString()}\{sourceFile.FileName}", sourceFile.SourceCode);
                }


                var calc = ServiceFactory.GetService<CalculationEngine>();
                var results = calc.GenerateAllocations(impl, scope).ToList();



                //OnTimerFired.Run(null, logger);







                //foreach (var file in Directory.GetFiles("SourceData").Where(x => x.ToLowerInvariant().EndsWith(".xlsx")))
                //{
                //    using (var stream = new FileStream(file, FileMode.Open))
                //    {
                //        await importer.GetSourceDataAsync(Path.GetFileName(file), stream, budgetDefinition.Id);
                //    }
                //}


                //if (compilerOutput.Success)
                //{
                //    var calc = ServiceFactory.GetService<CalculationEngine>();
                //    await calc.GenerateAllocations(compilerOutput);
                //}
                //else
                //{
                //    foreach (var compilerMessage in compilerOutput.CompilerMessages)
                //    {
                //        Console.WriteLine(compilerMessage.Message);
                //    }
                //    Console.ReadKey();
                //}

                // await StoreAggregates(budgetDefinition, new AllocationFactory(compilerOutput.Assembly));



            }

            // Do any async anything you need here without worry
        ).GetAwaiter().GetResult();
        }

        private static async Task<Specification> ImportSpecification(Reference user, ConsoleLogger logger)
        {
            var specJson = File.ReadAllText(Path.Combine("SourceData", "spec.json"));

            var spec = JsonConvert.DeserializeObject<Specification>(specJson);

            //await Specifications.RunCommands(GetHttpRequest(new SpecificationCommand
            //{
            //    Content = spec,
            //    Method = CommandMethod.Post,
            //    Id = Reference.NewId(),
            //    User = user
            //}), logger);

            return spec;
        }


        //private static async Task<SpecificationScope> ImportProviders(Specification specification, Reference user, ConsoleLogger logger)
        //{
        //    using (var blob = File.Open(Path.Combine("SourceData", "edubasealldata20171122.csv"), FileMode.Open))
        //    {

        //        var stopWatch = new Stopwatch();
        //        stopWatch.Start();

        //        using (var reader = new StreamReader(blob))
        //        {
        //           // var providers = new EdubaseImporterService().ImportEdubaseCsv(reader).ToList();


        //            //stopWatch.Stop();
        //            //Console.WriteLine($"Read {providers.Count} providers in {stopWatch.ElapsedMilliseconds}ms");
        //            //stopWatch.Restart();

        //            //var scope = new SpecificationScope
        //            //{
        //            //    Id = Reference.NewId(),
        //            //    Name = specification.Name,
        //            //    Specification = specification.GetReference(),
        //            //    Providers = providers.Select(x => new ProviderSummary
        //            //    {
        //            //        Id = x.Id,
        //            //        URN = x.URN,
        //            //        Name = x.Name,
        //            //        Authority = x.Authority,
        //            //        Phase = x.PhaseOfEducation,
        //            //        Tags = new List<string>()
        //            //    }).ToList()
        //            //};

        //            //await SpecificationScopes.RunCommands(GetHttpRequest(new SpecificationScopeCommand
        //            //{
        //            //    Content = scope,
        //            //    Method = CommandMethod.Post,
        //            //    Id = Reference.NewId(),
        //            //    User = user
        //            //}), logger);
        //            File.WriteAllText("scope.json", JsonConvert.SerializeObject(scope));
        //            return scope;



        //            //foreach (var provider in providers.Take(5))
        //            //{
        //            //    await Providers.RunCommands(GetHttpRequest(new ProviderCommand
        //            //    {
        //            //        Content = provider,
        //            //        Method = CommandMethod.Post,
        //            //        Id =  Reference.NewId(),
        //            //        User = user
        //            //    }), logger);
        //            //}
        //        }

        //    }
        //}

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
