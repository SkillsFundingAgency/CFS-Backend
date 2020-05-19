using CalculateFunding.Common.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Helpers
    {
        public static Message ConvertToMessage<T>(string body) where T: class
        {
            QueueMessage<T> queueMessage = null;

            if (body.IsBase64())
            {
                byte[] zippedBytes = Convert.FromBase64String(body);

                using MemoryStream inputStream = new MemoryStream(zippedBytes);
                using GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using StreamReader streamReader = new StreamReader(gZipStream);
                string decompressed = streamReader.ReadToEnd();

                queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(decompressed);
            }
            else
            {
                queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(body);
            }

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
