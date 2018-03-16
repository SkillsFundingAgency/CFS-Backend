using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ApiClientProxy : IApiClientProxy, IDisposable
    {
        private const string SfaCorellationId = "sfa-correlationId";
        private const string SfaUsernameProperty = "sfa-username";
        private const string SfaUserIdProperty = "sfa-userid";

        private const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public ApiClientProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _correlationIdProvider = correlationIdProvider;

            _httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            string baseAddress = options.ApiEndpoint;
            if (!baseAddress.EndsWith("/", StringComparison.CurrentCulture))
            {
                baseAddress = $"{baseAddress}/";
            }

            _httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
            _httpClient.DefaultRequestHeaders?.Add(OcpApimSubscriptionKey, options.ApiKey);
            _httpClient.DefaultRequestHeaders?.Add(SfaCorellationId, _correlationIdProvider.GetCorrelationId());
            _httpClient.DefaultRequestHeaders?.Add(SfaUsernameProperty, "testuser");
            _httpClient.DefaultRequestHeaders?.Add(SfaUserIdProperty, "b001af14-3754-4cb1-9980-359e850700a8");

            _httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        }

        public async Task<T> GetAsync<T>(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException(nameof(url));
            }

            HttpResponseMessage response = await RetryAgent.DoRequestAsync(() => _httpClient.GetAsync(url));

            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url = {_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            if (response.IsSuccessStatusCode)
            {
                string bodyContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(bodyContent, _serializerSettings);
            }

            return default(T);
        }

        public async Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            string json = JsonConvert.SerializeObject(request, _serializerSettings);
            //_logger.Debug($"ApiClient POST: {{url}} ({typeof(TRequest).Name})", url);

            HttpResponseMessage response = await RetryAgent.DoRequestAsync(() => _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")));
            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            return response.StatusCode;
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var json = JsonConvert.SerializeObject(request, _serializerSettings);
           // _logger.Debug($"ApiClient POST: {{url}} ({typeof(TRequest).Name} => {typeof(TResponse).Name})", url);
            HttpResponseMessage response = await RetryAgent.DoRequestAsync(() => _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")));
            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(responseBody, _serializerSettings);
            }

            return default(TResponse);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
