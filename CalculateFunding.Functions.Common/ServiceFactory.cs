using System;
using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Providers;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.CSharp;
using CalculateFunding.Services.Compiler.VisualBasic;
using CalculateFunding.Services.DataImporter;
using Microsoft.EntityFrameworkCore;
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
            
            Mapper.Initialize(cfg => cfg.CreateMap<ProviderEventEntity, ProviderIndex>());

            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(new LoggerFactory()
                    .AddConsole()
                    .AddSerilog()
                    .AddDebug())
                .AddLogging();
            ServiceProvider = serviceCollection
                .AddDbContext<ProvidersDbContext>(options => options.UseSqlServer(config["ProvidersConnectionString"], sqlServerOptions => sqlServerOptions.CommandTimeout(60 * 3)))
                .AddSingleton(new CosmosRepository(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = config["CosmosDBDatabaseName"],
                    CollectionName = config["CosmosDBCollectionName"]
                    
                }, null))
                .AddSingleton(new SearchRepository<ProviderIndex>(new SearchRepositorySettings
                {
                    SearchServiceName = config["SearchServiceName"],
                    SearchKey = config["SearchServicePrimaryKey"]
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
}
