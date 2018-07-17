using CalculateFunding.Api.Common.Middleware;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using CalculateFunding.Services.Scenarios.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Api.Scenarios
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            RegisterComponents(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseMvc();
        }

        public void RegisterComponents(IServiceCollection builder)
        {
            builder.AddSingleton<IScenariosRepository, ScenariosRepository>();
            builder.AddSingleton<IScenariosService, ScenariosService>();
            builder.AddSingleton<IScenariosSearchService, ScenariosSearchService>();

            builder
                .AddSingleton<IValidator<CreateNewTestScenarioVersion>, CreateNewTestScenarioVersionValidator>();
            builder
                .AddSingleton<ISpecificationsRepository, SpecificationsRepository>();

            builder
               .AddSingleton<IBuildProjectRepository, BuildProjectRepository>();

            builder.AddUserProviderFromRequest();

            builder.AddCalcsInterServiceClient(Configuration);
            builder.AddSpecificationsInterServiceClient(Configuration);

            builder.AddCosmosDb(Configuration);

            builder.AddSearch(Configuration);

            builder.AddServiceBus(Configuration);

            builder.AddCaching(Configuration);

            builder.AddApplicationInsightsTelemetryClient(Configuration);

            builder.AddLogging("CalculateFunding.Api.Scenarios");

            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings((IConfigurationRoot)Configuration);

            builder.AddHttpContextAccessor();
        }
    }
}
