using System;
using AutoMapper;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Users;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Services.Users.Validators;
using FluentValidation;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Functions.Users
{
    public static class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build(IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(config);
            }

            return _serviceProvider;
        }

        public static IServiceProvider BuildServiceProvider(IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static IServiceProvider Build(Message message, IConfigurationRoot config)
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = BuildServiceProvider(message, config);
            }

            IUserProfileProvider userProfileProvider = _serviceProvider.GetService<IUserProfileProvider>();

            Reference user = message.GetUserDetails();

            userProfileProvider.SetUser(user.Id, user.Name);

            return _serviceProvider;
        }

        public static IServiceProvider BuildServiceProvider(Message message, IConfigurationRoot config)
        {
            ServiceCollection serviceProvider = new ServiceCollection();

            serviceProvider.AddUserProviderFromMessage(message);

            RegisterComponents(serviceProvider, config);

            return serviceProvider.BuildServiceProvider();
        }

        public static void RegisterComponents(IServiceCollection builder, IConfigurationRoot config)
        {
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

            builder
                .AddSingleton<ISpecificationRepository, SpecificationRepository>();

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
                    SpecificationRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    UserRepositoryPolicy = CosmosResiliencePolicyHelper.GenerateCosmosPolicy(totalNetworkRequestsPolicy),
                };
            });

            builder.AddCosmosDb(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config, "CalculateFunding.Functions.Users");

            builder.AddLogging("CalculateFunding.Functions.Users");

            builder.AddTelemetry();

            builder.AddSpecificationsInterServiceClient(config);
        }
    }
}
