using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Functions.Jobs.ServiceBus;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Functions.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Jobs.Repositories;
using CalculateFunding.Services.Jobs.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Jobs.Startup))]

namespace CalculateFunding.Functions.Jobs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services, builder.GetFunctionsConfigurationToIncludeHostJson());
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfiguration azureFuncConfig = null)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig(azureFuncConfig);

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            // These registrations of the functions themselves are just for the DebugQueue. Ideally we don't want these registered in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddScoped<OnJobNotification>();
                builder.AddScoped<OnCheckForJobTimeout>();
            }

            builder.AddSingleton<IUserProfileProvider, UserProfileProvider>();

            builder.AddSingleton<IJobManagementService, JobManagementService>();

            builder
                .AddSingleton<IValidator<JobDefinition>, JobDefinitionValidator>();

            builder.
                AddSingleton<IValidator<CreateJobValidationModel>, CreateJobValidator>();

            builder
                .AddSingleton<IJobRepository, JobRepository>((ctx) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                    config.Bind("CosmosDbSettings", cosmosDbSettings);

                    cosmosDbSettings.ContainerName = "jobs";

                    CosmosRepository jobCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                    return new JobRepository(jobCosmosRepostory);
                });

            builder
               .AddSingleton<INotificationService, NotificationService>();

            builder
              .AddSingleton<IJobDefinitionsService, JobDefinitionsService>();

            builder
                .AddSingleton<IJobDefinitionsRepository, JobDefinitionsRepository>((ctx) =>
                {
                    CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                    config.Bind("CosmosDbSettings", cosmosDbSettings);

                    cosmosDbSettings.ContainerName = "jobdefinitions";

                    CosmosRepository jobDefinitionsCosmosRepostory = new CosmosRepository(cosmosDbSettings);

                    return new JobDefinitionsRepository(jobDefinitionsCosmosRepostory);
                });

            builder.AddServiceBus(config, "jobs");

            builder.AddPolicySettings(config);

            builder.AddCaching(config);

            builder.AddSingleton<IJobsResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);
                BulkheadPolicy totalNetworkRequestsPolicyNonAsync = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsNonAsyncPolicy(policySettings);

                return new ResiliencePolicies
                {
                    JobDefinitionsRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    JobRepository = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    MessengerServicePolicy = ResiliencePolicyHelpers.GenerateMessagingPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddApplicationInsightsServiceName(config, "CalculateFunding.Functions.Jobs");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Jobs");

            builder.AddLogging("CalculateFunding.Functions.Jobs");

            builder.AddTelemetry();

            builder.AddScoped<IUserProfileProvider, UserProfileProvider>();

            return builder.BuildServiceProvider();
        }
    }
}
