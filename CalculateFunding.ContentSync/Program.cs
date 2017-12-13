using CalculateFunding.ApiClient;
using System;
using System.IO.Pipes;
using System.Security.Authentication.ExtendedProtection;
using CalculateFunding.Models.Specs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.ContentSync
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args);

            var config = builder.Build();

            CalculateFundingApiOptions opt = new CalculateFundingApiOptions();
            config.Bind(opt);
            var services = new ServiceCollection();
            services.AddSingleton<CalculateFundingApiClient>(new CalculateFundingApiClient(opt));


            var provider = services.BuildServiceProvider();

            var apiClient = provider.GetService<CalculateFundingApiClient>();

            var result = apiClient.PostSpecification(new Specification
            {
                Id = "test",
                Name = "Test",

            }).Result;

            Console.WriteLine(result);
        }
    }
}
