using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CalculateFunding.Common.Identity.Authentication;
using CalculateFunding.Editor.FundingDataZone.Options;

namespace CalculateFunding.Editor.FundingDataZone.Modules
{
    public class AuthModule
    {
        private const string AuthenticationScheme = "EasyAuth";

        public IConfiguration Configuration { get; set; }

        public IWebHostEnvironment HostingEnvironment { get; set; }

        public void Configure(IServiceCollection services)
        {
            AzureAdOptions azureAdOptions = new AzureAdOptions();

            if (HostingEnvironment.IsDevelopment())
            {
                services.AddAuthorization();

                services.AddMvc();
            }
            else
            {
                Configuration.Bind("FdzAdOptions", azureAdOptions);

                services.AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = AuthenticationScheme;
                    opts.DefaultChallengeScheme = AuthenticationScheme;
                })
                .AddAzureAuthentication();

                services.AddAuthorization();

                services.AddMvc(config =>
                {
                    AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                                     .RequireAuthenticatedUser()
                                     .RequireClaim("groups", azureAdOptions.Groups?.Split(","))
                                     .Build();
                    config.Filters.Add(new AuthorizeFilter(policy));
                });
            }
        }
    }
}

