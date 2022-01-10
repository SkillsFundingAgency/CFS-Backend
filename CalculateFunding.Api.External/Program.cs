using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Api.External
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var startup = new Startup(builder.Configuration);
            builder.Host.ConfigureAppConfiguration();
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            startup.Configure(app, app.Lifetime, app.Environment, app.Services.GetService<IApiVersionDescriptionProvider>());
            app.Run();
        }
    }
}
