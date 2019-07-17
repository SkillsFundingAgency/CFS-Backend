using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Validators;
using Microsoft.Extensions.DependencyInjection;

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
            serviceCollection.AddTransient<ISpecificationPublishingService, SpecificationPublishingService>();
            serviceCollection.AddSingleton<IProviderFundingPublishingService, ProviderFundingPublishingService>();
            serviceCollection.AddTransient<IPublishSpecificationValidator, PublishSpecificationValidator>();
            serviceCollection.AddTransient<ICreateRefreshFundingJobs, RefreshFundingJobCreation>();
            serviceCollection.AddTransient<ICreatePublishFundingJobs, PublishProviderFundingJobCreation>();
            serviceCollection.AddTransient<ISpecificationsApiClient, SpecificationsApiClient>();
        }
    }
}