using CalculateFunding.Common.Sql;
using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Editor.FundingDataZone.Modules;
using CalculateFunding.Editor.FundingDataZone.Options;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.IoC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CalculateFunding.Editor.FundingDataZone
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services, IWebHostEnvironment environment)
        {
            AuthModule module = new();
            module.Configuration = Configuration;
            module.HostingEnvironment = environment;
            module.Configure(services);

            services.AddRazorPages();

            services.AddSingleton<ISqlPolicyFactory, SqlPolicyFactory>();
            services.AddCaching(Configuration);

            FundingDataZoneServiceIoCRegistrations.AddFundingDataZoneDBSettings(services, Configuration);
            FundingDataZoneServiceIoCRegistrations.AddFundingDataZoneServices(services, Configuration);
            
            services.AddScoped<IPublishingAreaEditorRepository, PublishingAreaEditorRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (!environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
