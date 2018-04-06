using CalculateFunding.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;
using CalculateFunding.Services.Core.EventHub;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class MessageExtensions
    {
        public static Reference GetUserDetails(this EventData message)
        {
            string userId = "8bcd2782-e8cb-4643-8803-951d715fc201";
            string userName = "system";

            if (message.Properties.ContainsKey("user-id"))
            {
                userId = message.Properties["user-id"].ToString();
            }

            if (message.Properties.ContainsKey("user-name"))
            {
                userName = message.Properties["user-name"].ToString();
            }

            return new Reference(userId, userName);
        }

        public static T GetPayloadAsInstanceOf<T>(this EventData message)
        {
            if (message.Body == null)
                return default(T);

            var json = Encoding.UTF8.GetString(message.Body.Array);

            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string GetCorrelationId(this EventData message)
        {
            string correlationId = Guid.NewGuid().ToString();
           
            if (message.Properties.ContainsKey("sfa-correlationId"))
            {
                correlationId = message.Properties["sfa-correlationId"].ToString();
            }

            return correlationId;
        }

        public static string GetMessageId(this EventData message)
        {
            if (message.Properties.ContainsKey(MessengerService.MessageIdPropertyName))
            {
                return message.Properties[MessengerService.MessageIdPropertyName]?.ToString();
            }

            return null;
        }

        public static IDictionary<string, string> BuildMessageProperties(this EventData message)
        {
            Reference user = message.GetUserDetails();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("sfa-correlationId", message.GetCorrelationId());

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }
    }
}
