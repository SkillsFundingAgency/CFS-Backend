using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CalculateFunding.Functions.FundingDataZone.StartUp))]

namespace CalculateFunding.Functions.FundingDataZone
{
    public class StartUp : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        public static void RegisterComponents(IServiceCollection serviceCollection,
            IConfigurationRoot createTestConfiguration)
        {
            //TODO;
        }
    }
}