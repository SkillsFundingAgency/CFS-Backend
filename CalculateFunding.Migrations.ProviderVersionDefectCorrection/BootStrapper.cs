using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Migrations.ProviderVersionDefectCorrection.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly.Bulkhead;

namespace CalculateFunding.Migrations.ProviderVersionDefectCorrection
{
    public class BootStrapper
    {
        private const string AppSettingsJson = "appsettings.json";

        private static readonly IConfigurationRoot _configuration  = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsJson)
            .Build();

        public static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddApplicationInsightsTelemetryClient(_configuration, "CalculateFunding.Migrations.ProviderVersionDefectCorrection");
            serviceCollection.AddLogging(serviceName: "CalculateFunding.Migrations.ProviderVersionDefectCorrection");
            serviceCollection.AddPolicySettings(_configuration);
            serviceCollection.AddSingleton<ICosmosRepository>(ctx =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                _configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "publishedfunding";

                return new CosmosRepository(cosmosDbSettings);   
            });
            serviceCollection.AddSingleton<IPublishingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    PublishedProviderVersionRepository = ResiliencePolicyHelpers
                        .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });
            
            serviceCollection.AddTransient<IProviderVersionMigration, ProviderVersionMigration>();
            
            return serviceCollection.BuildServiceProvider();
        }
    }
}