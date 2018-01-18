using CalculateFunding.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
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

        public static Reference GetUser(this HttpRequest request)
        {
            Reference reference = new Reference();

            Claim idClaim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Sid);

            if(idClaim != null)
            {
                reference.Id = idClaim.Value;
            }

            Claim nameClaim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Name);

            if (nameClaim != null)
            {
                reference.Name = nameClaim.Value;
            }

            return reference;
        }
    }
}
