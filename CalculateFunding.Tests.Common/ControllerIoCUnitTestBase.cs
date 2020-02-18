using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CalculateFunding.Tests.Common
{
    public abstract class ControllerIoCUnitTestBase
        : IoCUnitTestBaseFor<ControllerBase>
    {
        protected override void AddExtraRegistrations(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Substitute.For<IHostingEnvironment>());
            
            base.AddExtraRegistrations(serviceCollection);
        }
    }
}