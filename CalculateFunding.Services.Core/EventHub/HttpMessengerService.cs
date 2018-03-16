using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.EventHub
{
    public class HttpMessengerService : IMessengerService
    {
        private readonly IApiClientProxy _apiClient;

        public HttpMessengerService(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        async public Task SendAsync<T>(string hubName, T data, IDictionary<string, string> properties)
        {
            var json = JsonConvert.SerializeObject(data);

            EventData message = new EventData(Encoding.UTF8.GetBytes(json));

            foreach (var property in properties)
                message.Properties.Add(property.Key, property.Value);

            await RetryAgent.DoAsync(() => _apiClient.PostAsync($"events/{hubName}", message));
        }

        public Task SendBatchAsync<T>(string hubName, IEnumerable<T> data, IDictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }
    }
}
