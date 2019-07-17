using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Common.WebApi.Middleware;
using CalculateFunding.Services.Core.AspNet;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.IoC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Api.Publishing
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            RegisterComponents(services);
        }

        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggedInUserMiddleware>();

            app.UseMiddleware<ApiKeyMiddleware>();

            app.UseMvc();

            app.UseHealthCheckMiddleware();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddApplicationInsights(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Publishing");
            builder.AddLogging("CalculateFunding.Api.Publishing");
            builder.AddTelemetry();
            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot) Configuration);
            builder.AddPolicySettings(Configuration);
            builder.AddHttpContextAccessor();
            builder.AddHealthCheckMiddleware();
            builder.AddPublishingServices();
            builder.AddSpecificationsInterServiceClient(Configuration);
            builder.AddJobsInterServiceClient(Configuration);
            builder.AddSingleton<IPublishingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    JobsApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });
        }
    }
}