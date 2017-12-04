﻿using System;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using CalculateFunding.Services.Calculator;
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
                // .AddOptions()
                //.Configure<RepositorySettings>(settings => config.GetSection("SpecificationsRepository"))
                //.Configure<RepositorySettings>(settings => config.GetSection("DatasetsRepository"))
                //.Configure<RepositorySettings>(settings => config.GetSection("ResultsRepository"))
                //.Configure<RepositorySettings>(settings => config.GetSection("Configuration"))

                .AddSingleton(new Repository<Budget>(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = "calculate-funding",
                    CollectionName = "specs"
                }, null))
                .AddSingleton(new Repository<ProviderSourceDataset>(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = "calculate-funding",
                    CollectionName = "datasets",
                    PartitionKey = "/providerUrn"
                }, null))
                .AddSingleton(new Repository<ProviderResult>(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = "calculate-funding",
                    CollectionName = "results"
                }, null))
                .AddSingleton(new Repository<ProviderTestResult>(new RepositorySettings
                {
                    ConnectionString = config["CosmosDBConnectionString"],
                    DatabaseName = "calculate-funding",
                    CollectionName = "results"
                }, null))
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
