using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Services.Publishing.IoC
{
    public static class PublishingServiceIoCRegistrations
    {
        public static IServiceCollection AddPublishingServices(this IServiceCollection serviceCollection)
        {
            RegisterSpecificationServiceComponents(serviceCollection);

            return serviceCollection;
        }

        private static void RegisterSpecificationServiceComponents(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<ISpecificationPublishingService, SpecificationPublishingService>()
                .AddSingleton<IHealthChecker, SpecificationPublishingService>();

            serviceCollection
                .AddSingleton<IProviderFundingPublishingService, ProviderFundingPublishingService>()
                .AddSingleton<IHealthChecker, SpecificationPublishingService>();

            serviceCollection.AddTransient<IPublishSpecificationValidator, PublishSpecificationValidator>();
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
            serviceCollection.AddSingleton<ISpecificationsApiClient, SpecificationsApiClient>();
        }
    }
}