using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Helpers;

namespace CalculateFunding.Services.Core.Helpers
{
    public class EnvironmentProvider : IEnvironmentProvider
    {
        private readonly string _cfsEnvironmentConfig;

        public EnvironmentProvider(string cfsEnvironmentConfig)
        {
            _cfsEnvironmentConfig = cfsEnvironmentConfig;
        }

        public CFSEnvironment GetEnvironment()
        {
            return _cfsEnvironmentConfig switch
            {
                EnvironmentConstants.AppEnvironmentNames.Dev => CFSEnvironment.Dev,
                EnvironmentConstants.AppEnvironmentNames.Test => CFSEnvironment.Test,
                EnvironmentConstants.AppEnvironmentNames.Integration => CFSEnvironment.Integration,
                EnvironmentConstants.AppEnvironmentNames.Sandbox => CFSEnvironment.Sandbox,
                EnvironmentConstants.AppEnvironmentNames.PreProduction => CFSEnvironment.Preproduction,
                EnvironmentConstants.AppEnvironmentNames.Production => CFSEnvironment.Production,
                _ => CFSEnvironment.Dev,
            };
        }
    }
}
