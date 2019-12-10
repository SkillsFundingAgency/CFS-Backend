using Microsoft.FeatureManagement;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryFeatureManagerSnapshot : IFeatureManagerSnapshot
    {
        public Task<bool> IsEnabledAsync(string feature)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
        {
            throw new NotImplementedException();
        }
    }
}
