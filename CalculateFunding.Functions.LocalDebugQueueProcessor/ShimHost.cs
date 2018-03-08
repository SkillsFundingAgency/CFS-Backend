using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    public interface IShimHost
    {
        Task Register();
        Task Unregister();
    }
    public class ShimHost<T> : IShimHost where T : IEventProcessor, new()
    {
        private readonly EventProcessorHost _eventProcessorHost;
        public ShimHost(IConfigurationRoot config, string hubName)
        {
            EventHubSettings eventHubSettings = new EventHubSettings();
            config.Bind("EventHubSettings", eventHubSettings);

            AzureStorageSettings storageSettings = new AzureStorageSettings();
            config.Bind("AzureStorageSettings", storageSettings);

            _eventProcessorHost = new EventProcessorHost(
                hubName,
                PartitionReceiver.DefaultConsumerGroupName,
                eventHubSettings.EventHubConnectionString,
                storageSettings.ConnectionString,
                $"hub{Environment.UserName.ToLowerInvariant()}");

        }

        public async Task Register()
        {
            Console.WriteLine($"Registering {typeof(T).Name} on {_eventProcessorHost.EventHubPath}");
            // Registers the Event Processor Host and starts receiving messages
            await _eventProcessorHost.RegisterEventProcessorAsync<T>();

        }

        public async Task Unregister()
        {
            // Disposes of the Event Processor Host
            await _eventProcessorHost.UnregisterEventProcessorAsync();
        }
    }
}