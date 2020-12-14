using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public partial class JobServiceTests
    {
        public IJobService CreateJobService(
            IJobRepository jobRepository = null, 
            IMapper mapper = null,
            ICacheProvider cacheProvider = null)
        {
            return new JobService(
                jobRepository ?? CreateJobRepository(), 
                mapper ?? CreateMapper(), 
                JobsResilienceTestHelper.GenerateTestPolicies(),
                cacheProvider ?? CreateCacheProvider());
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private IJobRepository CreateJobRepository()
        {
            return Substitute.For<IJobRepository>();
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<JobsMappingProfile>();
            });

            return new Mapper(config);
        }

        public string NewRandomString() => new RandomString();
    }
}
