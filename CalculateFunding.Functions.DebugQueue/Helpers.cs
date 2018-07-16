using CalculateFunding.Services.Core.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Helpers
    {
        public static Message ConvertToMessage<T>(string body) where T: class
        {
            QueueMessage<T> queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(body);

            string data = JsonConvert.SerializeObject(queueMessage.Data);

            byte[] bytes = Encoding.UTF8.GetBytes(data);

            Message message = new Message(bytes);

            foreach(KeyValuePair<string, string> property in queueMessage.UserProperties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            return message;
        }
    }
}
