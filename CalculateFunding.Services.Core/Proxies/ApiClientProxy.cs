using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
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
        private readonly ILogger _logger;

        public ApiClientProxy(ApiOptions options, ILogger logger)
        {
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.IsNullOrWhiteSpace(options.ApiEndpoint, nameof(options.ApiEndpoint));
            Guard.IsNullOrWhiteSpace(options.ApiKey, nameof(options.ApiKey));
            Guard.ArgumentNotNull(logger, nameof(logger));

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
            _httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            _httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            _logger = logger;
        }

        #region "Get"
        private async Task<HttpResponseMessage> GetInternalAsync(string url)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response == null) throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{url}");

            return response;
        }

        public async Task<HttpStatusCode> GetAsync(string url)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpResponseMessage response = await GetInternalAsync(url);

            return response.StatusCode;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpResponseMessage response = await GetInternalAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string bodyContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(bodyContent, _serializerSettings);
            }

            return default;
        }
        #endregion "Get"

        #region "Post"
        private async Task<HttpResponseMessage> SendInternalAsync(HttpRequestMessage postRequest)
        {
            HttpResponseMessage response = await _httpClient.SendAsync(postRequest);

            if (response == null)
            {
                throw new HttpRequestException($"Unable to connect to server. Url={_httpClient.BaseAddress.AbsoluteUri}{postRequest.RequestUri}");
            }

            return response;
        }

        private HttpRequestMessage BuildPostRequest(string url, UserProfile userProfile)
        {
            HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, url);

            if (userProfile != null)
            {
                postRequest.Headers.Add(SfaUsernameProperty, userProfile.Name);
                postRequest.Headers.Add(SfaUserIdProperty, userProfile.Id);
            }

            return postRequest;
        }

        private async Task<HttpResponseMessage> PostInternalAsync<TRequest>(string url, TRequest request, UserProfile userProfile)
        {
            Guard.IsNullOrWhiteSpace(url, nameof(url));

            HttpRequestMessage postRequest = BuildPostRequest(url, userProfile);

            if (request != null)
            {
                string json = JsonConvert.SerializeObject(request, _serializerSettings);
                postRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await SendInternalAsync(postRequest);
        }

        public async Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request, UserProfile userProfile = null)
        {
            HttpResponseMessage response = await PostInternalAsync(url, request, userProfile);

            return response.StatusCode;
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, UserProfile userProfile = null)
        {
            HttpResponseMessage response = await PostInternalAsync(url, request, userProfile);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(responseBody, _serializerSettings);
            }

            return default;
        }

        public async Task<HttpStatusCode> PostAsync(string url, UserProfile userProfile = null)
        {
            return await PostAsync<string>(url, null, userProfile);
        }
        #endregion "Post"

        #region "Dispose"
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
        #endregion "Dispose"
    }
}

