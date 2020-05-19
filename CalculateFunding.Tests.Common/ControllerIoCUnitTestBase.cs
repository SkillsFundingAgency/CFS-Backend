using System.Linq;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CalculateFunding.Tests.Common
{
    public abstract class ControllerIoCUnitTestBase
        : IoCUnitTestBaseFor<ControllerBase>
    {
        protected override void AddExtraRegistrations()
        {
            ServiceCollection.AddScoped(_ => Substitute.For<IWebHostEnvironment>());

            ReplaceAllRegistrationsWith(Substitute.For<ITelemetry>(), ServiceLifetime.Scoped);
            ReplaceAllRegistrationsWith(Substitute.For<ITelemetryInitializer>(), ServiceLifetime.Singleton);
            ReplaceAllRegistrationsWith(Substitute.For<IConfigureOptions<ApplicationInsightsServiceOptions>>(), ServiceLifetime.Singleton);

            base.AddExtraRegistrations();
        }

        private void ReplaceAllRegistrationsWith<TService>(TService serviceInstance,
            ServiceLifetime serviceLifetime)
            where TService : class
        {
            ServiceDescriptor mockedServiceDescriptor = new ServiceDescriptor(typeof(TService),
                _ => serviceInstance,
                serviceLifetime);

            ServiceDescriptor[] actualTelemetryServices = ServiceCollection.Where(_ =>
                _.ServiceType == typeof(TService)).ToArray();

            foreach (ServiceDescriptor actualTelemetryService in actualTelemetryServices)
            {
                ServiceCollection.Remove(actualTelemetryService);
            }

            ServiceCollection.Add(mockedServiceDescriptor);
        }
    }
}