using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CalculateFunding.Services.Publishing.IoC
{
    public static class PublishingServiceIoCRegistrations
    {
        public static IServiceCollection AddPublishingServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            RegisterSpecificationServiceComponents(serviceCollection, configuration);

            return serviceCollection;
        }

        private static void RegisterSpecificationServiceComponents(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddSingleton<ISpecificationPublishingService, SpecificationPublishingService>();
            serviceCollection.AddSingleton<IProviderFundingPublishingService, ProviderFundingPublishingService>();
            serviceCollection.AddSingleton<IHealthChecker, ProviderFundingPublishingService>();
            serviceCollection.AddSingleton<ISpecificationIdServiceRequestValidator, PublishSpecificationValidator>();
            serviceCollection.AddSingleton<IPublishedProviderFundingService, PublishedProviderFundingService>();
            serviceCollection.AddSingleton<IHealthChecker, PublishedProviderFundingService>();
            serviceCollection.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>();
            serviceCollection.AddSingleton<ISpecificationService, SpecificationService>();
            serviceCollection.AddSingleton<IProviderService, ProviderService>();
            
            serviceCollection.AddSingleton<IPublishedFundingRepository, PublishedFundingRepository>(serviceProvider =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();
               
                configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.CollectionName = "calculationresults";

                return new PublishedFundingRepository(new CosmosRepository(cosmosDbSettings));
            });
            
            serviceCollection.AddTransient<ICreateJobsForSpecifications<RefreshFundingJobDefinition>>(ctx =>
            {
                return new JobCreationForSpecification<RefreshFundingJobDefinition>(ctx.GetService<IJobsApiClient>(), 
                    ctx.GetService<IPublishingResiliencePolicies>(), 
                    ctx.GetService<ILogger>(), 
                    new RefreshFundingJobDefinition());
            });
            serviceCollection.AddTransient<ICreateJobsForSpecifications<PublishProviderFundingJobDefinition>>(ctx =>
            {
                return new JobCreationForSpecification<PublishProviderFundingJobDefinition>(ctx.GetService<IJobsApiClient>(), 
                    ctx.GetService<IPublishingResiliencePolicies>(), 
                    ctx.GetService<ILogger>(), 
                    new PublishProviderFundingJobDefinition());
            });
            serviceCollection.AddTransient<ICreateJobsForSpecifications<ApproveFundingJobDefinition>>(ctx =>
            {
                return new JobCreationForSpecification<ApproveFundingJobDefinition>(ctx.GetService<IJobsApiClient>(),
                    ctx.GetService<IPublishingResiliencePolicies>(),
                    ctx.GetService<ILogger>(),
                    new ApproveFundingJobDefinition());
            });
        }
    }
}