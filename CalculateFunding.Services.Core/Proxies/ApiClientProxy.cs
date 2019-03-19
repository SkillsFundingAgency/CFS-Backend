using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace CalculateFunding.Services.Core.Proxies
{
    public class ApiClientProxy : IApiClientProxy, IDisposable
    {
        private const string SfaCorellationId = "sfa-correlationId";

        private const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        private const string SfaUsernameProperty = "sfa-username";
        private const string SfaUserIdProperty = "sfa-userid";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ILogger _logger;

        public ApiClientProxy(ApiOptions options, ILogger logger, ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.IsNullOrWhiteSpace(options.ApiEndpoint, nameof(options.ApiEndpoint));
            Guard.IsNullOrWhiteSpace(options.ApiKey, nameof(options.ApiKey));
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
            _httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _logger = logger;
        }

        public async Task<HttpStatusCode> GetAsync(string url)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            return response.StatusCode;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpResponseMessage response = await _httpClient.GetAsync(url);

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

        public async Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request, UserProfile userProfile = null)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            string json = JsonConvert.SerializeObject(request, _serializerSettings);

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            if (userProfile != null)
            {
                postRequest.Headers.Add(SfaUsernameProperty, userProfile.Name);
                postRequest.Headers.Add(SfaUserIdProperty, userProfile.Id);
            }

            HttpResponseMessage response = await _httpClient.SendAsync(postRequest);

            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            return response.StatusCode;
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, UserProfile userProfile = null)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            string json = JsonConvert.SerializeObject(request, _serializerSettings);

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            if (userProfile != null)
            {
                postRequest.Headers.Add(SfaUsernameProperty, userProfile.Name);
                postRequest.Headers.Add(SfaUserIdProperty, userProfile.Id);
            }

            HttpResponseMessage response = await _httpClient.SendAsync(postRequest);

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

        public async Task<HttpStatusCode> PostAsync(string url, UserProfile userProfile = null)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url);

            if (userProfile != null)
            {
                postRequest.Headers.Add(SfaUsernameProperty, userProfile.Name);
                postRequest.Headers.Add(SfaUserIdProperty, userProfile.Id);
            }

            HttpResponseMessage response = await _httpClient.SendAsync(postRequest);

            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");
            }

            return response.StatusCode;
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
