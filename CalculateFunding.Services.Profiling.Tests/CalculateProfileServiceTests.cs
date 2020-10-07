using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Services;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Profiling.Tests
{
    public partial class CalculateProfileServiceTests
    {
        private CalculateProfileService CreateService(
            IProfilePatternRepository profilePatternRepository = null,
            ICacheProvider cacheProvider = null,
            ILogger logger = null)
        {
            return new CalculateProfileService(
                profilePatternRepository ?? CreateProfilePatternRepository(),
                cacheProvider ?? Substitute.For<ICacheProvider>(),
                logger ?? CreateLogger(),
                new ProfilingResiliencePolicies
                {
                    Caching = Policy.NoOpAsync(),
                    ProfilePatternRepository = Policy.NoOpAsync()
                });
        }

        private IProfilePatternRepository CreateProfilePatternRepository()
        {
            return Substitute.For<IProfilePatternRepository>();
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
