using System.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;

namespace CalculateFunding.Api.External.Swagger.Helpers
{
    public static class Formatter
    {
        public static IActionResult ActionResult<TExampleProvider, TPayload>(HttpRequest request) where TExampleProvider : IExamplesProvider, new()
        {
            var exampleProvider = new TExampleProvider();
            var response = exampleProvider.GetExamples();
            if (request.Headers.TryGetValue("Accept", out var accept))
            {
                var mediaType = accept.FirstOrDefault()?.ToLowerInvariant();
                mediaType = mediaType?.Split(',').First().Trim();
                if (mediaType != null && mediaType.Contains("xml"))
                {
                    var stringwriter = new System.IO.StringWriter();
                    var serializer = new XmlSerializer(typeof(TPayload));
                    serializer.Serialize(stringwriter, response);
                    return new ContentResult()
                    {
                        StatusCode = 200,
                        Content = stringwriter.ToString(),
                        ContentType = mediaType
                    };
                }
                return new ContentResult()
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(response),
                    ContentType = mediaType
                };
            }

            return new BadRequestResult();
        }

        public static IActionResult ActionResult<TPayload>(HttpRequest request, TPayload payload)
        {
            if (request.Headers.TryGetValue("Accept", out var accept))
            {
                var mediaType = accept.FirstOrDefault()?.ToLowerInvariant();
                mediaType = mediaType?.Split(',').First().Trim();
                if (mediaType != null && mediaType.Contains("xml"))
                {
                    var stringwriter = new System.IO.StringWriter();
                    var serializer = new XmlSerializer(typeof(TPayload));
                    serializer.Serialize(stringwriter, payload);
                    return new ContentResult()
                    {
                        StatusCode = 200,
                        Content = stringwriter.ToString(),
                        ContentType = mediaType
                    };
                }
                return new ContentResult()
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(payload),
                    ContentType = mediaType
                };
            }

            return new BadRequestResult();
        }
    }
}
