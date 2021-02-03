using System;
using System.IO;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.WebApi.Extensions;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.ResiliencePolicies;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Polly.Bulkhead;
using Swashbuckle.AspNetCore.Filters;

namespace CalculateFunding.Api.Profiling
{
    public class Startup
    {
        private static readonly string AppConfigConnectionString = Environment.GetEnvironmentVariable("AzureConfiguration:ConnectionString");

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection builder)
        {
            IConfigurationSection azureADConfig = Configuration.GetSection("AzureAD");
            builder.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{azureADConfig.GetValue<string>("Authority")}/{azureADConfig.GetValue<string>("TenantId")}/";
                    options.Audience = azureADConfig.GetValue<string>("Audience");
                });

            builder.AddControllers()
                .AddNewtonsoftJson();

            RegisterComponents(builder);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (!string.IsNullOrEmpty(AppConfigConnectionString))
            {
                app.UseAzureAppConfiguration();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Calculation Funding Profiling");
                options.DocumentTitle = "Calculate Funding Profiling";
            });

            app.MapWhen(
                context => !context.Request.Path.Value.StartsWith("/swagger"),
                appBuilder =>
                {
                    appBuilder.UseRouting();
                    appBuilder.UseAuthentication();
                    appBuilder.UseAuthorization();
                    appBuilder.UseAuthenticatedHealthCheckMiddleware();
                    appBuilder.UseEndpoints(endpoints => { endpoints.MapControllers(); });
                });
        }

        private void RegisterComponents(IServiceCollection builder)
        {
            ConfigureSwaggerServices(builder, "Calculate Funding Profiling", "v1");

            builder.AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>();
            builder.AddSingleton<IProfilePatternRepository, ProfilePatternRepository>();
            builder.AddSingleton<IFundingValueProfiler, FundingValueProfiler>();
            builder.AddSingleton<IValidator<ProfileBatchRequest>, ProfileBatchRequestValidator>();

            builder.AddSingleton<IProfilePatternRepository, ProfilePatternRepository>(ctx =>
            {
                CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

                Configuration.Bind("CosmosDbSettings", cosmosDbSettings);

                cosmosDbSettings.ContainerName = "profiling";

                CosmosRepository resultsRepository = new CosmosRepository(cosmosDbSettings);

                return new ProfilePatternRepository(resultsRepository);
            });

            builder.AddSingleton<ICalculateProfileService, CalculateProfileService>()
                .AddSingleton<IProducerConsumerFactory, ProducerConsumerFactory>()
                .AddSingleton<IHealthChecker, CalculateProfileService>();
            builder.AddSingleton<ICosmosRepository, CosmosRepository>();

            builder.AddApplicationInsightsTelemetry();
            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Api.Profiling");
            builder.AddLogging("CalculateFunding.Api.Profiling");
            builder.AddAuthenticatedHealthCheckMiddleware();

            IConfiguration config = Configuration;

            RedisSettings redisSettings = new RedisSettings();

            config.Bind("redisSettings", redisSettings);

            builder.AddSingleton(redisSettings);

            builder
                .AddSingleton<ICacheProvider, StackExchangeRedisClientCacheProvider>();

            builder
                .AddScoped<IValidator<EditProfilePatternRequest>, UpsertProfilePatternValidator>();

            builder
                .AddScoped<IValidator<CreateProfilePatternRequest>, CreateProfilePatternValidator>();
            builder
                .AddScoped<IProfilePatternService, ProfilePatternService>();
            
            builder.AddScoped<IReprofilingService, ReProfilingService>();

            builder.AddScoped<IReprofilingService, ReProfilingService>();

            builder.AddScoped<IReprofilingStrategyListService, ReProfilingStrategyListService>();

            builder.AddSingleton<IProfilingResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = new PolicySettings();

                config.Bind("policy", policySettings);

                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ProfilingResiliencePolicies
                {
                    Caching = ResiliencePolicyHelpers.GenerateRedisPolicy(totalNetworkRequestsPolicy),
                    ProfilePatternRepository = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            builder.AddSingleton<IReProfilingStrategy, ReProfileDsgFundingLine>();
            builder.AddSingleton<IReProfilingStrategyLocator, ReProfilingStrategyLocator>();
        }

        public static void ConfigureSwaggerServices(IServiceCollection services,
            string title,
            string version)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(version,
                    new OpenApiInfo
                    {
                        Title = title,
                        Version = version
                    });

                options.OperationFilter<AddResponseHeadersFilter>();

                string xmlPath = Path.Combine(AppContext.BaseDirectory, "CalculateFunding.Api.Profiling.xml");

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            services.AddSwaggerGenNewtonsoftSupport();
        }
    }
}