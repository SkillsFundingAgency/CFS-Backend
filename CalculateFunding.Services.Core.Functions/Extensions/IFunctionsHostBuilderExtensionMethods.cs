using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.Core.Functions.Extensions
{
    public static class IFunctionsHostBuilderExtensionMethods
    {
        public static IConfiguration GetFunctionsConfigurationToIncludeHostJson(this IFunctionsHostBuilder functionsHostBuilder)
        {
            ServiceProvider tempServices = functionsHostBuilder.Services.BuildServiceProvider();
            return tempServices.GetService<IConfiguration>();
        }
    }
}
