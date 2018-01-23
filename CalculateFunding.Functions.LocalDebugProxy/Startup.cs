using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalculateFunding.Functions.LocalDebugProxy
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
            RegisterComponents(services);
            services.AddMvc();

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }

        void RegisterComponents(IServiceCollection builder)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            builder
                .AddScoped<ICalculationsRepository, CalculationsRepository>();

            builder.AddScoped<ICalculationsRepository, CalculationsRepository>((ctx) =>
            {
                CosmosDbSettings calssDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", calssDbSettings);

                calssDbSettings.CollectionName = "calcs";

                CosmosRepository calcsCosmosRepostory = new CosmosRepository(calssDbSettings);

                return new CalculationsRepository(calcsCosmosRepostory);
            });

            builder
               .AddScoped<ICalculationService, CalculationService>();

            builder
                .AddScoped<IValidator<Models.Calcs.Calculation>, CalculationModelValidator>();


            builder.AddScoped<ISpecificationsRepository, SpecificationsRepository>((ctx) =>
            {
                CosmosDbSettings specsDbSettings = new CosmosDbSettings();

                config.Bind("CosmosDbSettings", specsDbSettings);

                specsDbSettings.CollectionName = "specs";

                CosmosRepository specsCosmosRepostory = new CosmosRepository(specsDbSettings);

                return new SpecificationsRepository(specsCosmosRepostory);
            });



            builder
                .AddScoped<ISpecificationsService, SpecificationsService>();

            builder
                .AddScoped<IValidator<PolicyCreateModel>, PolicyCreateModelValidator>();

            builder
                .AddScoped<IValidator<CalculationCreateModel>, CalculationCreateModelValidator>();

            builder
                .AddScoped<IValidator<SpecificationCreateModel>, SpecificationCreateModelValidator>();

            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());

            builder.AddSingleton(mappingConfig.CreateMapper());

            builder.AddSearch(config);

            builder.AddServiceBus(config);

            builder.AddLogging(config, "CalculateFunding.Functions.LocalDebugProxy");
        }
    }
}
