using Microsoft.Extensions.Configuration;

namespace CalculateFunding.IntegrationTests.Common.Configuration
{
    public static class ConfigurationFactory
    {
        private const string BackEndSecretsKey = "df0d69d5-a6db-4598-909f-262fc39cb8c8";
        
        public static IConfiguration CreateConfiguration()
        {         
            return new ConfigurationBuilder()
                .AddUserSecrets(BackEndSecretsKey)
                .AddEnvironmentVariables()
                .Build();
        }          
    }
}