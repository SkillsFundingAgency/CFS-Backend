using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Http;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class HttpRequestExtensions
    {
        public static async Task<string> GetRawBodyStringAsync(this HttpRequest request, Encoding encoding = null)
        {
            if (request.Body == null) return string.Empty;

            using (StreamReader reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8))
            {
                if (request.Body.CanSeek)
                {
                    request.Body.Seek(0, SeekOrigin.Begin);
                }

                return await reader.ReadToEndAsync();
            }
        }

        public static string GetYamlFileNameFromRequest(this HttpRequest request)
        {
            return request.GetHeaderKey("yaml-file") ?? "File name not provided";
        }

        public static string GetJsonFileNameFromRequest(this HttpRequest request)
        {
            return request.GetHeaderKey("json-file") ?? "File name not provided";
        }

        public static string GetCorrelationId(this HttpRequest request)
        {
            const string sfaCorrelationId = "sfa-correlationId";

            if (request.Headers.ContainsKey(sfaCorrelationId))
            {
                return request.Headers[sfaCorrelationId].FirstOrDefault();
            }

            return Guid.NewGuid().ToString();
        }

        public static string GetHeaderKey(this HttpRequest request, string key)
        {
            return request.Headers.ContainsKey(key)
                ? request.Headers[key].FirstOrDefault()
                : null;
        }

        public static Reference GetUser(this HttpRequest request)
        {
            Reference reference = new Reference();

            string id = request.GetClaimValue(ClaimTypes.Sid);

            if (id != null) reference.Id = id;

            string name = request.GetClaimValue(ClaimTypes.Name);

            if (name != null) reference.Name = name;

            return reference;
        }

        public static Reference GetUserOrDefault(this HttpRequest request)
        {
            Reference reference = new Reference("default", "defaultName");

            string id = request.GetClaimValue(ClaimTypes.Sid);

            if (!string.IsNullOrWhiteSpace(id)) reference.Id = id;

            string name = request.GetClaimValue(ClaimTypes.Name);

            if (!string.IsNullOrWhiteSpace(name)) reference.Name = name;

            return reference;
        }

        private static string GetClaimValue(this HttpRequest request, string claimType)
        {
            Claim claim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == claimType);

            return claim?.Value;
        }
    }
}
