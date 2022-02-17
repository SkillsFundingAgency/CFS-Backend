using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Functions.DebugQueue
{
    /// <summary>
    /// Creates all of the storage queues based on the QueueTrigger attribute.
    /// This reduces the amount of resources based on getting 404's back from the storage emulator
    /// </summary>
    public class LocalQueueCreateTimer
    {
        private const string EveryDay = "0 0 * * *";
        private readonly IConfiguration _configuration;

        public LocalQueueCreateTimer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("LocalQueueCreate")]
        public void CreateStorageQueuesDefinedInDebugQueue([TimerTrigger(EveryDay, RunOnStartup = true)] TimerInfo timerInfo)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            TypeInfo[] assemblyDefinedTypes = assembly.DefinedTypes.ToArray();

            string queueConnectionString = _configuration.GetValue<string>("AzureWebJobsStorage");
            if (string.IsNullOrWhiteSpace(queueConnectionString))
            {
                throw new InvalidOperationException("queueConnectionString is null or empty");
            }

            foreach (TypeInfo typeInfo in assemblyDefinedTypes)
            {
                foreach (MethodInfo methodInfo in typeInfo.GetMethods())
                {
                    foreach (ParameterInfo parameter in methodInfo.GetParameters())
                    {
                        string queueName = GetAttributeProperty(parameter.CustomAttributes, "QueueTrigger", "QueueName");
                        if (!string.IsNullOrWhiteSpace(queueName))
                        {
                            QueueClient client = new QueueClient(queueConnectionString, queueName);
                            Azure.Response response = client.CreateIfNotExists();
                            if (response != null)
                            {
                                Console.WriteLine($"Created queue '{queueName}'");
                            }
                        }
                    }

                }
            }
        }

        public static string GetAttributeProperty(IEnumerable<CustomAttributeData> customAttibutes, string attributeName, string propertyName)
        {
            CustomAttributeData property = customAttibutes.Where(c => c.AttributeType.Name == $"{attributeName}Attribute").SingleOrDefault();
            if (property != null)
            {
                if (property.ConstructorArguments.Any())
                {
                    return property.ConstructorArguments.First().Value?.ToString();
                }

            }

            return null;
        }
    }
}
