using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public partial class JobServiceTests
    {
        public IJobService CreateJobService(IJobRepository jobRepository = null, IMapper mapper = null)
        {
            return new JobService(jobRepository ?? CreateJobRepository(), mapper ?? CreateMapper(), JobsResilienceTestHelper.GenerateTestPolicies());
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
    }
}
