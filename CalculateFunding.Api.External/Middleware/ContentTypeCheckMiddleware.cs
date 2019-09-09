using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace CalculateFunding.Api.External.Middleware
{
    public class ContentTypeCheckMiddleware
    {
        private static readonly List<string> AcceptableContentTypes = new List<string>()
        {
            "application/atom+json",
            "application/json"
        };

        private readonly RequestDelegate _next;

        public ContentTypeCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method.ToUpper() == "GET" && context.Request.Path.Value.StartsWith("/api/v"))
            {
                List<string> contentHeaders = context.Request.Headers[$"{HeaderNames.Accept}"].ToList();

                bool isHeaderAcceptable = contentHeaders.All(h => AcceptableContentTypes.Contains(h));
                if (!isHeaderAcceptable)
                {
                    context.Response.StatusCode = 406;
                    return;
                }
            }
            await _next.Invoke(context);
        }
    }
}
