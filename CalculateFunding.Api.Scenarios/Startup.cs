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

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

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

            builder.AddCalcsInterServiceClient(config);
            builder.AddSpecificationsInterServiceClient(config);

            builder.AddCosmosDb(config);

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddCaching(config);

            builder.AddApplicationInsightsTelemetryClient(config);

            builder.AddLogging("CalculateFunding.Api.Scenarios");

            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings(config);

            builder.AddHttpContextAccessor();
        }
    }
}
