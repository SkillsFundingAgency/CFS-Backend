using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Functions.Users.ServiceBus;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Users;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Services.Users.Validators;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.Users.Startup))]

namespace CalculateFunding.Functions.Users
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterComponents(builder.Services);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            return RegisterComponents(builder, config);
        }

        public static IServiceProvider RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
            return Register(builder, config);
        }

        private static IServiceProvider Register(IServiceCollection builder, IConfigurationRoot config)
        {
            builder
                .AddSingleton<OnEditSpecificationEvent>();

            builder
              .AddSingleton<IUserService, UserService>()
              .AddSingleton<IHealthChecker, UserService>();

            builder
               .AddSingleton<IFundingStreamPermissionService, FundingStreamPermissionService>()
               .AddSingleton<IHealthChecker, FundingStreamPermissionService>();

            builder.AddSingleton<IValidator<UserCreateModel>, UserCreateModelValidator>();

            builder.AddSingleton<IUserRepository, UserRepository>((ctx) =>
            {
                CosmosDbSettings usersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", usersDbSettings);

                usersDbSettings.CollectionName = "users";

                CosmosRepository usersCosmosRepostory = new CosmosRepository(usersDbSettings);

                return new UserRepository(usersCosmosRepostory);
            });

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddSingleton<IVersionRepository<FundingStreamPermissionVersion>, VersionRepository<FundingStreamPermissionVersion>>((ctx) =>
            {
                CosmosDbSettings versioningDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", versioningDbSettings);

                versioningDbSettings.CollectionName = "users";

                CosmosRepository versioningRepository = new CosmosRepository(versioningDbSettings);

                return new VersionRepository<FundingStreamPermissionVersion>(versioningRepository);
            });

            builder.AddPolicySettings(config);

            builder.AddSingleton<IUsersResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new UsersResiliencePolicies
                {
                    FundingStreamPermissionVersionRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                    CacheProviderPolicy = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    SpecificationApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    UserRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddCosmosDb(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsForFunctionApps(config, "CalculateFunding.Functions.Users");
            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Users");
            builder.AddLogging("CalculateFunding.Functions.Users");

            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(config);

            return builder.BuildServiceProvider();
        }
    }
}
