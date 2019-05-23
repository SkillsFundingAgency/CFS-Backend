using System.Collections.Generic;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Calculator;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Proxies;
using CalculateFunding.Services.Providers;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CalculateFunding.DebugAllocationModel
{
    class Program
    {
        static void Main(string[] args)
        {
            Serilog.ILogger logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            IFeatureToggle featureToggle = new FeatureToggles();

            //string specificationId = "a1fe8998-406b-44b8-92c7-502a560e7b6e";
            ////string providerId = "10027545";
            //string providerId = "10004758";

            string specificationId = "b1952bc1-4ed9-4ae1-b29d-c72d8d22e830";
            //string providerId = "10027545";
            string providerId = "10063088";

            IConfigurationRoot config = ConfigHelper.AddConfig();

            EngineSettings engineSettings = new EngineSettings();

            CosmosDbSettings dbSettings = new CosmosDbSettings();
            config.Bind("CosmosDbSettings", dbSettings);
            dbSettings.CollectionName = "providerdatasets";
            CosmosRepository calcsCosmosRepostory = new CosmosRepository(dbSettings);
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository = new ProviderSourceDatasetsRepository(calcsCosmosRepostory, engineSettings);


            RedisSettings redisSettings = new RedisSettings();
            config.Bind("redisSettings", redisSettings);
            ICacheProvider cacheProvider = new StackExchangeRedisClientCacheProvider(redisSettings);

            ApiOptions apiOptions = new ApiOptions();

            config.Bind("resultsClient", apiOptions);

            ICorrelationIdProvider correlationIdProvider = new CorrelationIdProvider();

            IResultsApiClientProxy resultsApi = new ResultsApiProxy(apiOptions, logger, correlationIdProvider);

            IMapper mapper = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderMappingProfile>();
            }).CreateMapper();

            ProviderService providerService = new ProviderService(cacheProvider, resultsApi, mapper);

            AllocationModelDebugRunner debugger = new AllocationModelDebugRunner(logger, featureToggle, providerSourceDatasetsRepository, providerService);

            (IEnumerable<Models.Results.CalculationResult> calculationResults, long ms) = debugger.Execute(specificationId, providerId).Result;

            CalculationRunSummaryGenerator summaryGenerator = new CalculationRunSummaryGenerator();
            summaryGenerator.GenerateSummary(calculationResults, ms, specificationId, providerId);
        }
    }
}
