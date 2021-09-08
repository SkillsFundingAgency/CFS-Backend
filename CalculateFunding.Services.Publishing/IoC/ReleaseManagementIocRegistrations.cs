using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.ReleaseManagement;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.Publishing.IoC
{
    public static class ReleaseManagementIocRegistrations
    {
        public static IServiceCollection AddReleaseManagementServices(this IServiceCollection builder, IConfiguration configuration)
        {
            builder.AddSingleton<IExternalApiQueryBuilder, ExternalApiQueryBuilder>();

            builder.AddSingleton<IReleaseManagementRepository, ReleaseManagementRepository>((svc) =>
            {
                ISqlSettings sqlSettings = new SqlSettings();

                configuration.Bind("releaseManagementSql", sqlSettings);
                SqlConnectionFactory factory = new SqlConnectionFactory(sqlSettings);

                SqlPolicyFactory sqlPolicyFactory = new SqlPolicyFactory();
                IExternalApiQueryBuilder externalApiQueryBuilder = svc.GetService<IExternalApiQueryBuilder>();
                return new ReleaseManagementRepository(factory, sqlPolicyFactory, externalApiQueryBuilder);
            });

            builder.AddScoped<IChannelOrganisationGroupChangeDetector, ChannelOrganisationGroupChangeDetector>();
            builder.AddScoped<IChannelOrganisationGroupGeneratorService, ChannelOrganisationGroupGeneratorService>();
            builder.AddSingleton<IChannelsService, ChannelsService>();
            builder.AddSingleton<IValidator<ChannelRequest>, ChannelModelValidator>();

            builder.AddSingleton<IProvidersForChannelFilterService, ProvidersForChannelFilterService>();
            builder.AddScoped<IPublishedProvidersLoadContext, PublishedProvidersLoadContext>();
            builder.AddScoped<IReleaseApprovedProvidersService, ReleaseApprovedProvidersService>();
            builder.AddScoped<IReleaseProvidersToChannelsService, ReleaseProvidersToChannelsService>();
            builder.AddScoped<IReleaseToChannelSqlMappingContext, ReleaseToChannelSqlMappingContext>();

            return builder;
        }
    }
}
