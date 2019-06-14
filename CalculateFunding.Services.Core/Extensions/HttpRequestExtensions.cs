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
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            if (request.Body != null)
            {
                using (StreamReader reader = new StreamReader(request.Body, encoding))
                {
                    if (request.Body.CanSeek)
                    {
                        request.Body.Seek(0, SeekOrigin.Begin);
                    }

                    return await reader.ReadToEndAsync();
                }
            }

            return string.Empty;
        }

        public static string GetYamlFileNameFromRequest(this HttpRequest request)
        {
            if (request.Headers.ContainsKey("yaml-file"))
            {
                return request.Headers["yaml-file"].FirstOrDefault();
            }

            return "File name not provided";
        }

        public static string GetJsonFileNameFromRequest(this HttpRequest request)
        {
            if (request.Headers.ContainsKey("json-file"))
            {
                return request.Headers["json-file"].FirstOrDefault();
            }

            return "File name not provided";
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

        public static Reference GetUser(this HttpRequest request)
        {
            Reference reference = new Reference();

            Claim idClaim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Sid);

            if (idClaim != null)
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

        public static Reference GetUserOrDefault(this HttpRequest request)
        {
            Reference reference = new Reference("default", "defaultName");

            Claim idClaim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Sid);

            if (idClaim != null && !string.IsNullOrWhiteSpace(idClaim.Value))
            {
                reference.Id = idClaim.Value;
            }

            Claim nameClaim = request.HttpContext.User.Claims.FirstOrDefault(m => m.Type == ClaimTypes.Name);

            if (nameClaim != null && !string.IsNullOrWhiteSpace(nameClaim.Value))
            {
                reference.Name = nameClaim.Value;
            }

            return reference;
        }
    }
}
