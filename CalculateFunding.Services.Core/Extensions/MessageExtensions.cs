using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class MessageExtensions
    {
        public static Reference GetUserDetails(this Message message)
        {
            string userId = message.GetUserProperty<string>("user-id");
            string userName = message.GetUserProperty<string>("user-name");

            if(string.IsNullOrWhiteSpace(userId)) userId = "8bcd2782-e8cb-4643-8803-951d715fc201";
            if(string.IsNullOrWhiteSpace(userName)) userName = "system";
            
            return new Reference(userId, userName);
        }

        public static T GetPayloadAsInstanceOf<T>(this Message message)
        {
            if (message.Body == null)
            {
                return default;
            }

            string json = GetMessageBodyStringFromMessage(message);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string GetCorrelationId(this Message message)
        {
            string correlationId = message.GetUserProperty<string>("sfa-correlationId");

            return string.IsNullOrWhiteSpace(correlationId) 
                ? Guid.NewGuid().ToString()
                : correlationId;
        }

        public static IDictionary<string, string> BuildMessageProperties(string correlationId, Reference user)
        {
            return MessagePropertiesInternal(correlationId, user);
        }

        private static IDictionary<string, string> MessagePropertiesInternal(string correlationId, Reference user)
        {
            IDictionary<string, string> properties = new Dictionary<string, string>
            {
                { "sfa-correlationId", correlationId }
            };

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        public static IDictionary<string, string> BuildMessageProperties(this Message message)
        {
            return MessagePropertiesInternal(message.GetCorrelationId(), message.GetUserDetails());
        }

        public static IDictionary<string, string> BuildMessageProperties(this HttpRequest request)
        {
            return MessagePropertiesInternal(request.GetCorrelationId(), request.GetUser());
        }

        public static string GetMessageBodyStringFromMessage(Message message)
        {
            string json = "";

            if (message.UserProperties.ContainsKey("compressed"))
            {
                using MemoryStream inputStream = new MemoryStream(message.Body);
                using GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using StreamReader streamReader = new StreamReader(gZipStream);
                json = streamReader.ReadToEnd();
            }
            else
            {
                json = Encoding.UTF8.GetString(message.Body);
            }

            return json;
        }

        public static T GetUserProperty<T>(this Message message, string key) 
        {
            return (!message.UserProperties.ContainsKey(key) || message.UserProperties[key] == null)
                ? default
                : (T)message.UserProperties[key];
        }
    }
}
