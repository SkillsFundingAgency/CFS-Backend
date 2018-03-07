using CalculateFunding.Models;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class MessageExtensions
    {
        public static Reference GetUserDetails(this Message message)
        {
            string userId = "8bcd2782-e8cb-4643-8803-951d715fc201";
            string userName = "system";

            if (message.UserProperties.ContainsKey("user-id"))
            {
                userId = message.UserProperties["user-id"].ToString();
            }

            if (message.UserProperties.ContainsKey("user-name"))
            {
                userName = message.UserProperties["user-name"].ToString();
            }

            return new Reference(userId, userName);
        }

        public static T GetPayloadAsInstanceOf<T>(this Message message)
        {
            if (message.Body == null)
                return default(T);

            var json = Encoding.UTF8.GetString(message.Body);

            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string GetCorrelationId(this Message message)
        {
            string correlationId = Guid.NewGuid().ToString();
           
            if (message.UserProperties.ContainsKey("sfa-correlationId"))
            {
                correlationId = message.UserProperties["sfa-correlationId"].ToString();
            }

            return correlationId;
        }

        public static IDictionary<string, string> BuildMessageProperties(this Message message)
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
