using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Tests.Common
{
    public abstract class FunctionIoCUnitTestBase 
        : IoCUnitTestBaseFor<SmokeTest>
    {
        protected override void AddExtraRegistrations()
        {
            ServiceCollection.AddScoped(_ => AppConfigurationHelper.CreateConfigurationRefresherProvider());
        }
    }
}