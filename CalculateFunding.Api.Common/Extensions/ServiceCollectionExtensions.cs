using CalculateFunding.Api.Common.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Api.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHealthCheckMiddleware(this IServiceCollection services)
        {
            services.AddTransient<HealthCheckMiddleware>();

            return services;
        }
    }
}
