﻿using CalculateFunding.Services.Jobs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Jobs
{
    public static class JobsResilienceTestHelper
    {
        public static IJobsResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                JobDefinitionsRepository = Policy.NoOpAsync(),
                CacheProviderPolicy = Policy.NoOpAsync(),
                MessengerServicePolicy = Policy.NoOpAsync(),
                JobRepository = Policy.NoOpAsync()
            };
        }
    }
}
