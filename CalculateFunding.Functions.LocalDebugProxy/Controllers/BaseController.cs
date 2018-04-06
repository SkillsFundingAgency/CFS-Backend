﻿using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class BaseController : Controller
    {
        private readonly IServiceProvider _serviceProvider;

        public BaseController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected void SetUserAndCorrelationId(HttpRequest request)
        {
            ICorrelationIdProvider correlationIdProvider = _serviceProvider.GetService<ICorrelationIdProvider>();

            var correlationId = request.GetCorrelationId();

            correlationIdProvider.SetCorrelationId(correlationId);

            if (!request.HttpContext.Response.Headers.ContainsKey("sfa-correlationId"))
                request.HttpContext.Response.Headers.Add("sfa-correlationId", correlationId);
            else if (request.HttpContext.Response.Headers.ContainsKey("sfa-correlationId") && string.IsNullOrWhiteSpace(request.HttpContext.Response.Headers["sfa-correlationId"]))
                request.HttpContext.Response.Headers["sfa-correlationId"] = correlationId;

            string userId = "unknown";
            string username = "unknown";

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-userid"))
                userId = request.HttpContext.Request.Headers["sfa-userid"];

            if (request.HttpContext.Request.Headers.ContainsKey("sfa-username"))
                username = request.HttpContext.Request.Headers["sfa-username"];

            request.HttpContext.User = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, userId), new Claim(ClaimTypes.Name, username) })
            });
        }
    }
}
