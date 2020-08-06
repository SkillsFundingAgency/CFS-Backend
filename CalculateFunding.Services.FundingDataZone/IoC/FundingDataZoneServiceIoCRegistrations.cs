using AutoMapper;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.MappingProfiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.FundingDataZone.IoC
{
    public static class FundingDataZoneServiceIoCRegistrations
    {
        public static IServiceCollection AddFundingDataZoneServices(
            this IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            RegisterFundingDataZoneServiceComponents(serviceCollection, configuration);

            return serviceCollection;
        }

        private static void RegisterFundingDataZoneServiceComponents(
            IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            serviceCollection.AddSingleton(ctx => configuration);

            MapperConfiguration fdzConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingDataZoneMappingProfiles>();
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
            serviceCollection.AddSingleton<ISqlPolicyFactory, SqlPolicyFactory>();

            AddFundingDataZoneDBSettings(serviceCollection, configuration);

            serviceCollection.AddScoped<IHealthChecker, PublishingAreaRepository>();
        }

        public static IServiceCollection AddFundingDataZoneDBSettings(IServiceCollection builder, IConfiguration config)
        {
            ISqlSettings sqlSettings = new SqlSettings();

            config.Bind("FDZSqlStorageSettings", sqlSettings);

            builder.AddSingleton(sqlSettings);

            builder.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            builder.AddScoped<IPublishingAreaRepository, PublishingAreaRepository>();

            return builder;
        }
    }
}
