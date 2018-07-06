using AutoMapper;
using CalculateFunding.Api.Common.Middleware;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Services.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Api.Specs
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

            app.UseMiddleware<ApiKeyMiddleware>();

            app.UseHttpsRedirection();

            app.UseMiddleware<LoggedInUserMiddleware>();

            app.UseMvc();
        }

        static public void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder.AddScoped<ISpecificationsRepository, SpecificationsRepository>();
            builder.AddScoped<ISpecificationsService, SpecificationsService>();
            builder.AddScoped<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();
            builder.AddScoped<IValidator<PolicyEditModel>, PolicyEditModelValidator>();
            builder.AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();
            builder.AddScoped<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();
            builder.AddScoped<IValidator<CalculationEditModel>, CalculationEditModelValidator>();
            builder.AddScoped<IValidator<SpecificationEditModel>, SpecificationEditModelValidator>();
            builder.AddScoped<IValidator<AssignDefinitionRelationshipMessage>, AssignDefinitionRelationshipMessageValidator>();
            builder.AddScoped<ISpecificationsSearchService, SpecificationsSearchService>();
            builder.AddScoped<IResultsRepository, ResultsRepository>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddCosmosDb(config);

            builder.AddServiceBus(config);

            builder.AddSearch(config);

            builder.AddCaching(config);

            builder.AddResultsInterServiceClient(config);

            builder.AddPolicySettings(config);

            builder.AddApplicationInsightsTelemetryClient(config);
            builder.AddLogging("CalculateFunding.Apis.Specs");
            builder.AddTelemetry();

            builder.AddApiKeyMiddlewareSettings(config);

            builder.AddHttpContextAccessor();
        }
    }
}
