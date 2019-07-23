using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace CalculateFunding.Api.Providers
{
    public class Program
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
                 .UseKestrel((context, options) =>
                 {
                     options.Limits.MaxRequestBodySize = 104857600;
                 })
                .UseStartup<Startup>()
                .Build();
    }
}
