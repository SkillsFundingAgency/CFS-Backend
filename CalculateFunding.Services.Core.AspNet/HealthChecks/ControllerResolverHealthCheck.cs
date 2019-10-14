using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.AspNetCore.Mvc;

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
                Name = "Service Resolution Checks"
            };

            foreach (Type controllerType in controllerTypes)
            {
                try
                {
                    _serviceProvider.GetService(controllerType);

                    serviceHealth.Dependencies.Add(new DependencyHealth
                    {
                        DependencyName = controllerType.GetFriendlyName(),
                        HealthOk = true
                    });
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

            return Task.FromResult(serviceHealth);
        }

        private static Type[] GetControllerTypesInCurrentAppDomain()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(_ => _.GetTypes())
                .Where(_ => !_.IsAbstract &&
                            _.IsSubclassOf(typeof(Controller)))
                .ToArray();
        }
    }
}