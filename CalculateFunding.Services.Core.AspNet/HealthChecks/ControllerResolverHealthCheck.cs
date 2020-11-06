using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.Core.AspNet.HealthChecks
{
    public class ControllerResolverHealthCheck : IHealthChecker
    {
        private static readonly Lazy<Type[]> ControllerTypesAccessor = new Lazy<Type[]>(GetControllerTypesInCurrentAppDomain,
            LazyThreadSafetyMode.ExecutionAndPublication);

        private readonly IServiceProvider _serviceProvider;

        public ControllerResolverHealthCheck(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            Type[] controllerTypes = ControllerTypesAccessor.Value;
            ServiceHealth serviceHealth = new ServiceHealth
            {
                Name = "Service Resolution Checks",
            };

            foreach (Type controllerType in controllerTypes)
            {
                try
                {

                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        scope.ServiceProvider.GetService(controllerType);

                        serviceHealth.Dependencies.Add(new DependencyHealth
                        {
                            DependencyName = controllerType.GetFriendlyName(),
                            HealthOk = true
                        });
                    }

                }
                catch (Exception exception)
                {
                    serviceHealth.Dependencies.Add(new DependencyHealth
                    {
                        DependencyName = controllerType.GetFriendlyName(),
                        HealthOk = false,
                        Message = $"{exception.Message}{Environment.NewLine}{exception.StackTrace}"
                    });
                }
            }

            //if there are no controllers then we're healthy I guess as the common component for this doesn't handle empty dependency lists
            if (!serviceHealth.Dependencies.Any())
            {
                serviceHealth.Dependencies.Add(new DependencyHealth
                {
                    DependencyName = "No controller types located to resolve",
                    HealthOk = true
                });
            }

            return Task.FromResult(serviceHealth);
        }

        private static Type[] GetControllerTypesInCurrentAppDomain()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(_ => !_.FullName.StartsWith("Microsoft"))
                .SelectMany(_ => _.GetTypes())
                .Where(_ => !_.IsAbstract &&
                            _.IsSubclassOf(typeof(ControllerBase)))//We have some controllers that don't inherit Controller but instead use ControllerBase
                .ToArray();
        }
    }
}