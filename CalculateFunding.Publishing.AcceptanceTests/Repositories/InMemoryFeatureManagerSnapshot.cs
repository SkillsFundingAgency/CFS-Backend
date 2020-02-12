using Microsoft.FeatureManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryFeatureManagerSnapshot : IFeatureManagerSnapshot
    {
        readonly IDictionary<string, bool> _featureFlags = new Dictionary<string, bool>();

        public void SetIsEnabled(string feature, bool isEnabled)
        {
            _featureFlags[feature] = isEnabled;
        } 

        public Task<bool> IsEnabledAsync(string feature)
        {
            return Task.FromResult(_featureFlags.TryGetValue(feature, out bool flag) && flag);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            return IsEnabledAsync(feature);
        }
    }
}
