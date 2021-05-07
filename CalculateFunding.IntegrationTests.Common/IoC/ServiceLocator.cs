using System;
using CalculateFunding.Common.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.IntegrationTests.Common.IoC
{
    public class ServiceLocator
    {
        private IServiceProvider _serviceProvider;

        private ServiceLocator()
        {
            ServiceCollection = new ServiceCollection();
        }

        public IServiceCollection ServiceCollection { get; }

        public static ServiceLocator Create(IConfiguration configuration,
            params Action<IServiceCollection, IConfiguration>[] setUps)
        {
            Guard.ArgumentNotNull(configuration, nameof(configuration));

            ServiceLocator serviceLocator = new ServiceLocator();

            foreach (Action<IServiceCollection, IConfiguration> setUp in setUps)
            {
                setUp(serviceLocator.ServiceCollection, configuration);
            }

            serviceLocator.BuildServiceProvider();

            return serviceLocator;
        }

        private void BuildServiceProvider()
            => _serviceProvider = ServiceCollection.BuildServiceProvider();

        public TService GetService<TService>()
            where TService : class
            => _serviceProvider?.GetService<TService>();
    }
}