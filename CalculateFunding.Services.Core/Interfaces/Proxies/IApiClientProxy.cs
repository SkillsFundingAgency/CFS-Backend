using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;

namespace CalculateFunding.Services.Core.Interfaces.Proxies
{
    [Obsolete]
    public interface IApiClientProxy
    {
        Task<HttpStatusCode> GetAsync(string url);

        Task<T> GetAsync<T>(string url);

        Task<HttpStatusCode> PostAsync<TRequest>(string url, TRequest request, UserProfile userProfile = null);

        Task<TResponse> PostAsync<TResponse, TRequest>(string url, TRequest request, UserProfile userProfile = null);

        Task<HttpStatusCode> PostAsync(string url, UserProfile userProfile = null);
    }
}
