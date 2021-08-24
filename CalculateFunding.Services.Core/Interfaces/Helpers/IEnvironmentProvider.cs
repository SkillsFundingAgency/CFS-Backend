using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Core.Interfaces.Helpers
{
    public interface IEnvironmentProvider
    {
        CFSEnvironment GetEnvironment();
    }
}
