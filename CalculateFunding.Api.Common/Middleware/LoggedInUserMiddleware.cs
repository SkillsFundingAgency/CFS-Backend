using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CalculateFunding.Api.Common.Middleware
{
    public class LoggedInUserMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggedInUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            string userId = "unknown";
            string username = "unknown";

            if (context.Request.HttpContext.Request.Headers.ContainsKey("sfa-userid"))
                userId = context.Request.HttpContext.Request.Headers["sfa-userid"];

            if (context.Request.HttpContext.Request.Headers.ContainsKey("sfa-username"))
                username = context.Request.HttpContext.Request.Headers["sfa-username"];

            context.Request.HttpContext.User = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, userId), new Claim(ClaimTypes.Name, username) })
            });

            // Call the next delegate/middleware in the pipeline
            return this._next(context);
        }
    }
}
