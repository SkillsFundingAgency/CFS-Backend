namespace CalculateFunding.Generators.NavFeed
{
    using CalculateFunding.Generators.NavFeed.Options;
    using CommandLine;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Threading.Tasks;

    public class Program
    {
        async static Task Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            Startup startup = new Startup();
            startup.ConfigureServices(services);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var v1ProviderDocumentGenerator = serviceProvider.GetService<Providers.v1.ProviderDocumentGenerator>();
            var v2ProviderDocumentGenerator = serviceProvider.GetService<Providers.v2.ProviderDocumentGenerator>();

            Console.WriteLine("Starting generation of requested NAV Data. Please wait for Operation Completed message.");

            await Parser.Default.ParseArguments<FeedOptions>(args)
                .MapResult(
                opts => opts.FileVersion == 1 ? 
                v1ProviderDocumentGenerator.Generate(opts) : 
                v2ProviderDocumentGenerator.Generate(opts),
                errs => Task.FromResult(-1));

            Console.WriteLine();
            Console.WriteLine("Operation completed. Please press any key to stop this console application.");
            Console.ReadKey();
        }
    }
}
