using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.ApiClient
{
    public class CalculateFundingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private readonly string _resultsPath;
        private readonly string _specsPath;

        public CalculateFundingApiClient(CalculateFundingApiOptions options)
        {
            _httpClient = new HttpClient() { BaseAddress = new Uri(options.ApiEndpoint) };
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", options.ApiKey);
            _resultsPath = options.ResultsPath ?? "/api/results";
            _specsPath = options.SpecsPath ?? "/api/specs";
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>(response.StatusCode, JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), _serializerSettings));
            }
            return new ApiResponse<T>(response.StatusCode);
        }

        public async Task<ApiResponse<BudgetSummary[]>> GetBudgetResults()
        {
            return await GetAsync<BudgetSummary[]>($"{_resultsPath}/budgets");
        }

        public async Task<ApiResponse<Specification[]>> GetSpecifications(string academicYearId = null)
        {
            return await GetAsync<Specification[]>($"{_specsPath}/specifications?academicYearId={academicYearId}");
        }

        public async Task<ApiResponse<Specification>> GetSpecification(string id)
        {
            return await GetAsync<Specification>($"{_specsPath}/specifications?specificationId={id}");
        }


        public async Task<HttpStatusCode> PostSpecificationCommand(SpecificationCommand command)
        {
            return await PostAsync($"{_specsPath}/commands/specifications", command);
        }

        //public async Task<ApiResponse<ProviderTestResult[]>> GetProviderResults(string budgetId)
        //{
        //    return await GetAsync<ProviderTestResult[]>($"{_resultsPath}/providers?budgetId={budgetId}");
        //}

        //public async Task<ApiResponse<ProviderTestResult>> GetProviderResult(string budgetId, string providerId)
        //{
        //    return await GetAsync<ProviderTestResult>($"{_resultsPath}/providers?budgetId={budgetId}&providerId={providerId}");
        //}


        //public async Task<HttpStatusCode> PostProduct(string budgetId, Product product)
        //{
        //    return await PostAsync($"{_specsPath}/products?budgetId={budgetId}", product);
        //}

        //public async Task<ApiResponse<Product>> GetProduct(string budgetId, string productId)
        //{
        //    return await GetAsync<Product>($"{_specsPath}/products?budgetId={budgetId}&productId={productId}");
        //}

        //public async Task<ApiResponse<AllocationLine>> GetAllocationLine(string budgetId, string allocationLineId)
        //{
        //    return await GetAsync<AllocationLine>($"{_resultsPath}/allocationLine?budgetId={budgetId}&allocationLineId={allocationLineId}");
        //}


        //public async Task<ApiResponse<Specification[]>> GetBudgets()
        //{
        //    return await GetAsync<Specification[]>($"{_specsPath}/budgets");
        //}

        //public async Task<ApiResponse<PreviewResponse>> PostPreview(PreviewRequest request)
        //{

        //    return (await PostAsync<PreviewResponse, PreviewRequest>("api/v1/engine/preview", request));
        //}


        private async Task<ApiResponse<TResponse>> PostAsync<TResponse, TRequest>(string url, TRequest request)
        {
            string json = JsonConvert.SerializeObject(request, _serializerSettings);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<TResponse>(response.StatusCode, JsonConvert.DeserializeObject<TResponse>(await response.Content.ReadAsStringAsync(), _serializerSettings));
            }
            return new ApiResponse<TResponse>(response.StatusCode);
        }

        private async Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request)
        {
            string json = JsonConvert.SerializeObject(request, _serializerSettings);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            return response.StatusCode;
        }

    }
}