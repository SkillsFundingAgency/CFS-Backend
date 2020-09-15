using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Middleware;
using Microsoft.AspNetCore.Http;

namespace CalculateFunding.Api.External.Middleware
{
    public class AuthenticatedHealthCheckMiddleware : HealthCheckMiddleware, IMiddleware
    {
        public AuthenticatedHealthCheckMiddleware(IEnumerable<IHealthChecker> healthCheckers) : base(healthCheckers)
        {
        }

        public new async Task InvokeAsync(HttpContext context,
            RequestDelegate next)
        {
            if (context.Request.Path == (PathString) "/healthcheck")
            {
                if ((context.User?.Identity?.IsAuthenticated).GetValueOrDefault() == false)
                {
                    context.Response.StatusCode = 401;
                }
                else
                {
                    await base.InvokeAsync(context, next);
                }
            }
            else
                await next(context);
        }
    }
}