using System;
using System.IO;
using System.Reflection;

namespace CalculateFunding.Tests.Common.Helpers
{
    public static class EmbeddedResourceExtensions
    {
        public static string GetEmbeddedResourceFileContents(this Assembly assembly,
            string resourcePath)
        {
            using Stream stream = assembly.GetManifestResourceStream(resourcePath);

            if (stream == null)
                throw new InvalidOperationException(
                    $"Could not load manifest resource stream from {assembly.FullName} at requested path {resourcePath}");

            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}