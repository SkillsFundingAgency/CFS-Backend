
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.External.Swagger.Helpers.Readers
{
    public static class SwaggerTopContentReader
    {
        private const string DocsTopContentFilePath = "Swagger\\DocsTopContents.txt";

        public static string ReadContents()
        {
	        string currentDomainBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
	        string domainBaseAndSwaggerPath = $"{currentDomainBaseDirectory}{DocsTopContentFilePath}";
            if (!domainBaseAndSwaggerPath.EndsWith("txt") || !File.Exists(domainBaseAndSwaggerPath))
            {
                throw new FileNotFoundException($"File was not found or not correct extension - {domainBaseAndSwaggerPath}");
            }

	        return File.ReadAllText(domainBaseAndSwaggerPath);
        }
    }
}