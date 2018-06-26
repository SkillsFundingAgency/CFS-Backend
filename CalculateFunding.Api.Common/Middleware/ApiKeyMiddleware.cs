using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CalculateFunding.Api.Common.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _password;

        private const string HeaderKey = "Ocp-Apim-Subscription-Key";

        public ApiKeyMiddleware(RequestDelegate next, ApiKeyMiddlewareOptions options)
        {
            _next = next;

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException("API Key is null or empty string");
            }

            _password = options.ApiKey;
        }

        public Task InvokeAsync(HttpContext context)
        {
            bool isAuthorised = false;
            if (context.Request.Headers.ContainsKey(HeaderKey))
            {
                string providedKey = context.Request.Headers[HeaderKey];
                isAuthorised = providedKey == _password;
            }

            if (isAuthorised)
            {
                // Call the next delegate/middleware in the pipeline
                return this._next(context);
            }
            else
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
        }
    }
}
