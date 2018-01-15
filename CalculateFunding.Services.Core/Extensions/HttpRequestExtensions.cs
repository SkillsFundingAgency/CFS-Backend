using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async Task<string> GetRawBodyStringAsync(this HttpRequest request, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            using (StreamReader reader = new StreamReader(request.Body, encoding))
            {
                if(request.Body.CanSeek)
                    request.Body.Seek(0, SeekOrigin.Begin);

                return await reader.ReadToEndAsync();
            }   
        }

        public static string GetCorrelationId(this HttpRequest request)
        {
            const string sfaCorellationId = "sfa-correlationId";

            if (request.Headers.ContainsKey(sfaCorellationId))
            {
                return request.Headers[sfaCorellationId].FirstOrDefault();
            }

            return string.Empty;
        }
    }
}
