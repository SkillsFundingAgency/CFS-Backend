using AutoMapper;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.MappingProfiles;
using CalculateFunding.Services.FundingDataZone.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.FundingDataZone.IoC
{
    public static class FDZServiceIoCRegistrations
    {
        public static IServiceCollection AddFDZServices(
            this IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            RegisterFDZServiceComponents(serviceCollection, configuration);

            return serviceCollection;
        }

        private static void RegisterFDZServiceComponents(
            IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            serviceCollection.AddSingleton(ctx => configuration);

            MapperConfiguration fdzConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<FDZMappingProfiles>();
            });

            serviceCollection.AddSingleton(fdzConfig.CreateMapper());

            serviceCollection.AddScoped<IDataDownloadService, DataDownloadService>();
            serviceCollection.AddScoped<IDatasetsForFundingStreamService, DatasetsForFundingStreamService>();
            serviceCollection.AddScoped<IFundingStreamsWithDatasetsService, FundingStreamsWithDatasetsService>();
            serviceCollection.AddScoped<IFundingStreamsWithProviderSnapshotsRetrievalService, FundingStreamsWithProviderSnapshotsRetrievalService>();
            serviceCollection.AddScoped<ILocalAuthorityRetrievalService, LocalAuthorityRetrievalService>();
            serviceCollection.AddScoped<IOrganisationsRetrievalService, OrganisationsRetrievalService>();
            serviceCollection.AddScoped<IProviderRetrievalService, ProviderRetrievalService>();
            serviceCollection.AddScoped<IProvidersInSnapshotRetrievalService, ProvidersInSnapshotRetrievalService>();
            serviceCollection.AddScoped<IProviderSnapshotForFundingStreamService, ProviderSnapshotForFundingStreamService>();
            serviceCollection.AddScoped<IPublishingAreaRepository, PublishingAreaRepository>();

            AddFDZDBSettings(serviceCollection, configuration);

            serviceCollection.AddScoped<IHealthChecker, PublishingAreaRepository>();
        }

        public static IServiceCollection AddFDZDBSettings(IServiceCollection builder, IConfiguration config)
        {
            FDZSqlStorageSettings sqlStorageSettings = new FDZSqlStorageSettings();

            config.Bind("FDZSqlStorageSettings", sqlStorageSettings);

            builder.AddSingleton(sqlStorageSettings);

            builder.AddScoped<IPublishingAreaRepository, PublishingAreaRepository>();

            return builder;
        }
    }
}
