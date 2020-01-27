using CalculateFunding.Common.Config.ApiClient.Providers;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.Schema10;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Bulkhead;

namespace CalculateFunding.Generators.NavFeed
{
    internal class Startup
    {
        IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

        internal IServiceCollection ConfigureServices(IServiceCollection builder)
        {
            builder.AddTransient<IPublishedProviderContentsGenerator, PublishedProviderContentsGenerator>();
            builder.AddTransient<IPublishedFundingContentsGenerator, PublishedFundingContentsGenerator>();
            builder.AddTransient<Providers.v1.ProviderDocumentGenerator>();
            builder.AddTransient<Providers.v2.ProviderDocumentGenerator>();

            builder.AddApplicationInsightsTelemetryClient(Configuration, "CalculateFunding.Generators.NavFeed");
            builder.AddLogging("CalculateFunding.Generators.NavFeed");

            builder.AddProvidersInterServiceClient(Configuration);

            builder.AddPolicySettings(Configuration);
            builder.AddSingleton<IOrganisationGroupResiliencePolicies>((ctx) =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();

                BulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers.GenerateTotalNetworkRequestsPolicy(policySettings);

                return new OrganisationGroupResiliencePolicies
                {
                    ProvidersApiClient = ResiliencePolicyHelpers.GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            return builder;
        }
    }
}
