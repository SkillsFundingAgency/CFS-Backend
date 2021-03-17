using CalculateFunding.Common.Utility;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Extensions
{
    public static class ResourceHelper
    {
        public static string GetResourceContent(string resourceArea, string fileName)
        {
            Guard.IsNullOrWhiteSpace(fileName, nameof(fileName));
            Guard.IsNullOrWhiteSpace(resourceArea, nameof(resourceArea));

            string resourceName = $"CalculateFunding.Publishing.AcceptanceTests.Resources.{resourceArea}.{fileName}";

            string result = typeof(ResourceHelper)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException($"Unable to find resource for filename '{fileName}'");
            }

            return result;
        }
    }
}
