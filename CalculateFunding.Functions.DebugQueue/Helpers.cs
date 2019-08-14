using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CalculateFunding.Services.Core.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Helpers
    {
        public static Message ConvertToMessage<T>(string body) where T : class
        {
            QueueMessage<T> queueMessage = null;

            if (body.IsBase64())
            {
                queueMessage = QueueMessageFromBase64<T>(body);
            }
            else
            {
                queueMessage = JsonConvert.DeserializeObject<QueueMessage<T>>(body);
            }

            return MessageFromQueueMessage(queueMessage);
        }

        public static Message MessageFromQueueMessage<T>(QueueMessage<T> queueMessage) where T : class
        {
            string data = queueMessage?.Data == null
                ? ""
                : JsonConvert.SerializeObject(queueMessage?.Data);

            byte[] bytes = Encoding.UTF8.GetBytes(data);

            Message message = new Message(bytes);

            if (queueMessage != null)
            {
                foreach (KeyValuePair<string, string> property in queueMessage?.UserProperties)
                {
                    message.UserProperties.Add(property.Key, property.Value);
                }
            }

            return message;
        }

        public static QueueMessage<T> QueueMessageFromBase64<T>(string body) where T : class
        {
            byte[] zippedBytes = Convert.FromBase64String(body);

            using (MemoryStream inputStream = new MemoryStream(zippedBytes))
            {
                using (GZipStream gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (StreamReader streamReader = new StreamReader(gZipStream))
                    {
                        string decompressed = streamReader.ReadToEnd();

                        return JsonConvert.DeserializeObject<QueueMessage<T>>(decompressed);
                    }
                }
            }
        }
    }
}
