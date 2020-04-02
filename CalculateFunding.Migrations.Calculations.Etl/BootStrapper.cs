using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Bulkhead;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public class BootStrapper
    {
        private const string AppSettingsJson = "appsettings.json";

        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
            .AddJsonFile(AppSettingsJson)
            .Build();
        
        private static readonly TimeSpan[] RetryTimeSpans =
        {
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5)
        };

        private const int NumberOfExceptionsBeforeCircuitBreaker = 100;

        private static readonly TimeSpan CircuitBreakerFailurePeriod = TimeSpan.FromMinutes(1);

        public static IServiceProvider BuildServiceProvider(IApiOptions options)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging("CalculateFunding.Migrations.ProviderVersionDefectCorrection");
            serviceCollection.AddPolicySettings(Configuration);
            serviceCollection.AddSingleton<ICalculationsEtlResiliencePolicies>(ctx =>
            {
                PolicySettings policySettings = ctx.GetService<PolicySettings>();
                AsyncBulkheadPolicy totalNetworkRequestsPolicy = ResiliencePolicyHelpers
                    .GenerateTotalNetworkRequestsPolicy(policySettings);

                return new ResiliencePolicies
                {
                    CalculationsApiClient = GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    SpecificationApiClient = GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy),
                    DataSetsApiClient = GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy)
                };
            });

            AddHttpClientForClientKey(HttpClientKeys.Specifications,
                options.SpecificationsApiUri,
                options.SpecificationsApiKey,
                serviceCollection);
            AddHttpClientForClientKey(HttpClientKeys.Calculations,
                options.CalculationsApiUri,
                options.CalculationsApiKey,
                serviceCollection);
            AddHttpClientForClientKey(HttpClientKeys.Datasets,
                options.DataSetsApiUri,
                options.DataSetsApiKey,
                serviceCollection);

            serviceCollection.AddSingleton<ICalculationsApiClient, CalculationsApiClient>();
            serviceCollection.AddTransient<ISpecificationsApiClient, SpecificationsApiClient>();
            serviceCollection.AddTransient<IDatasetsApiClient, DatasetsApiClient>();
            
            return serviceCollection.BuildServiceProvider();
        }

        private static AsyncPolicy GenerateRestRepositoryPolicy(AsyncBulkheadPolicy totalNetworkRequestsPolicy)
        {
            return ResiliencePolicyHelpers
                .GenerateRestRepositoryPolicy(totalNetworkRequestsPolicy);
        }

        private static void AddHttpClientForClientKey(string clientKey,
            string uri,
            string key,
            IServiceCollection serviceCollection)
        {
            serviceCollection.AddHttpClient(clientKey,
                    c =>
                    {
                        ApiClientConfigurationOptions opts = new ApiClientConfigurationOptions
                        {
                            ApiEndpoint = uri,
                            ApiKey = key
                        };

                        SetDefaultApiClientConfigurationOptions(c, opts);
                    })
                .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(RetryTimeSpans))
                .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(NumberOfExceptionsBeforeCircuitBreaker,
                    CircuitBreakerFailurePeriod));
        }

        private static void SetDefaultApiClientConfigurationOptions(HttpClient httpClient,
            ApiClientConfigurationOptions options)
        {
            if (options.ApiEndpoint.IsNullOrWhitespace()) throw new InvalidOperationException("options EndPoint is null or empty string");

            var baseAddress = options.ApiEndpoint;

            if (!baseAddress.EndsWith("/", StringComparison.CurrentCulture)) baseAddress = $"{baseAddress}/";

            httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
            httpClient.DefaultRequestHeaders?.Add(ApiClientHeaders.ApiKey, options.ApiKey);
            httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        }
    }
}