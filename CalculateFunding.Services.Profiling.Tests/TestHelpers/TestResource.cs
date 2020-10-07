namespace CalculateFunding.Services.Profiling.Tests.TestHelpers
{
    using System;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;

    public class TestResource : IDisposable
    {
        public TestResource(string nameSpace, string resourceName)
        {
            Stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"{nameSpace}.{resourceName}");
        }

        public Stream Stream { get; }

        public void Dispose()
        {
            Stream?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static T FromJson<T>(string nameSpace, string resourceName)
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"{nameSpace}.{resourceName}"))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd());
            }
        }
    }

}