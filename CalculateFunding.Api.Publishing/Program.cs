using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CalculateFunding.Api.Publishing
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    ConfigHelper.LoadConfiguration(config);
                })
                .UseStartup<Startup>()
                .Build();
    }
}
