
using System;
using System.IO;

namespace CalculateFunding.Api.External.Swagger.Helpers.Readers
{
    public static class SwaggerTopContentReader
    {
        public static string ReadContents(int version)
        {
            string currentDomainBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string domainBaseAndSwaggerPath = $"{currentDomainBaseDirectory}\\Swagger\\DocsTopContents.v{version}.md";
            if (!domainBaseAndSwaggerPath.EndsWith("md") || !File.Exists(domainBaseAndSwaggerPath))
            {
                throw new FileNotFoundException($"File was not found or not correct extension - {domainBaseAndSwaggerPath}");
            }

            return File.ReadAllText(domainBaseAndSwaggerPath);
        }
    }
}