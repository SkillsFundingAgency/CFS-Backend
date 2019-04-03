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
            string userId = "8bcd2782-e8cb-4643-8803-951d715fc201";
            string userName = "system";

            if (message.UserProperties.ContainsKey("user-id") && !string.IsNullOrWhiteSpace(message.UserProperties["user-id"]?.ToString()))
            {
                userId = message.UserProperties["user-id"].ToString();
            }

            if (message.UserProperties.ContainsKey("user-name") && !string.IsNullOrWhiteSpace(message.UserProperties["user-name"]?.ToString()))
            {
                userName = message.UserProperties["user-name"].ToString();
            }

            return new Reference(userId, userName);
        }

        public static T GetPayloadAsInstanceOf<T>(this Message message)
        {
            if (message.Body == null)
            {
                return default(T);
            }

            string json = GetMessageBodyStringFromMessage(message);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string GetCorrelationId(this Message message)
        {
            string correlationId = Guid.NewGuid().ToString();

            if (message.UserProperties.ContainsKey("sfa-correlationId") && message.UserProperties["sfa-correlationId"] != null)
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

        public static IDictionary<string, string> BuildMessageProperties(this HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>
            {
                { "sfa-correlationId", request.GetCorrelationId() }
            };

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        public static string GetMessageBodyStringFromMessage(Message message)
        {
            string json = "";

            if (message.UserProperties.ContainsKey("compressed"))
            {
                using (MemoryStream inputStream = new MemoryStream(message.Body))
                {
                    using (GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        using (StreamReader streamReader = new StreamReader(gZipStream))
                        {
                            json = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            else
            {
                json = Encoding.UTF8.GetString(message.Body);
            }

            return json;
        }
    }
}
