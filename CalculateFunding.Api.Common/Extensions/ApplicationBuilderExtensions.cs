using CalculateFunding.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;

namespace CalculateFunding.Api.Common.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHealthCheckMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HealthCheckMiddleware>();
        }
    }
}
