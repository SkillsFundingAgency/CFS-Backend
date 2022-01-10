using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Builder;

namespace CalculateFunding.Api.Datasets
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
            startup.Configure(app, app.Environment);
            app.Run();
        }
    }
}
