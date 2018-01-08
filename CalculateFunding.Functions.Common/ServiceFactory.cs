using System;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.CSharp;
using CalculateFunding.Services.Compiler.VisualBasic;
using CalculateFunding.Services.DataImporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CalculateFunding.Functions.Common
{
    public static class ServiceFactory
    {
        private static ServiceProvider ServiceProvider { get; }

        public static T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }
        static ServiceFactory()
        {
            var vars = Environment.GetEnvironmentVariables();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true)
                .AddJsonFile("appsettings.json", optional:true)
                .AddEnvironmentVariables();

            var config = builder.Build();

           

            var serviceCollection = new ServiceCollection()
                .AddSingleton(new LoggerFactory()
                    .AddConsole()
                    .AddSerilog()
                    .AddDebug())
                .AddLogging();
            ServiceProvider = serviceCollection
                .AddSingleton(new CosmosRepository(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = config["CosmosDBDatabaseName"],
                    CollectionName = config["CosmosDBCollectionName"]
                    
                }))
                .AddSingleton<IMessenger>(new Messenger(config["ServiceBusConnectionString"]))
                .AddSingleton(new MessagePump(config["ServiceBusConnectionString"]))
                .AddSingleton(new SearchRepository<ProviderIndex>(new SearchRepositorySettings
                {
                    SearchServiceName = config["SearchServiceName"],
                    SearchKey = config["SearchServiceKey"]
                }))
               
                .AddTransient<CSharpCompiler>()
                .AddTransient<VisualBasicCompiler>()
                .AddTransient<BudgetCompiler>()
                .AddTransient<DataImporterService>()
                .AddTransient<CalculationEngine>()
                .BuildServiceProvider();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ApplicationInsightsTraces(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"))
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .CreateLogger();
        }
    }

    internal class FakeMessenger : IMessenger
    {

        public async Task SendAsync<T>(string topicName, T command)
        {
           // throw new NotImplementedException();
        }
    }
}
