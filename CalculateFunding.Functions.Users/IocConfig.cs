using System;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Users;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Users
{
    static public class IocConfig
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Build()
        {
            if (_serviceProvider == null)
                _serviceProvider = BuildServiceProvider();

            return _serviceProvider;
        }

        static public IServiceProvider BuildServiceProvider()
        {
            var serviceProvider = new ServiceCollection();

            RegisterComponents(serviceProvider);

            return serviceProvider.BuildServiceProvider();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder
                .AddSingleton<IUserService, UserService>();

            builder.AddSingleton<IUserRepository, UserRepository>((ctx) =>
            {
                CosmosDbSettings usersDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", usersDbSettings);

                usersDbSettings.CollectionName = "users";

                CosmosRepository usersCosmosRepostory = new CosmosRepository(usersDbSettings);

                return new UserRepository(usersCosmosRepostory);
            });

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                builder.AddCosmosDb(config, "users");
            }
            else
            {
                builder.AddCosmosDb(config);
            }

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);

            builder.AddLogging("CalculateFunding.Functions.Users");

            builder.AddTelemetry();
        }
    }
}
