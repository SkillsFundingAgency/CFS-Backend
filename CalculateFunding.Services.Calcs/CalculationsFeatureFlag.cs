using CalculateFunding.Services.Calcs.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationsFeatureFlag : ICalculationsFeatureFlag
    {
        private readonly IFeatureManagerSnapshot _featureManager;

        public CalculationsFeatureFlag(IFeatureManagerSnapshot featureManager)
        {
            _featureManager = featureManager;
        }

        public async Task<bool> IsGraphEnabled()
        {
            return await _featureManager.IsEnabledAsync("EnableGraph");
        }
    }
}
