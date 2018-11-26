using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    [TestClass]
    public partial class JobManagementServiceTests
    {
        private JobManagementService CreateJobManagementService(
            IJobRepository jobRepository = null,
            INotificationService notificationService = null,
            IJobDefinitionsService jobDefinitionsService = null,
            IJobsResiliencePolicies resilliencePolicies = null,
            ILogger logger = null)
        {
            return new JobManagementService(
                    jobRepository ?? CreateJobRepository(),
                    notificationService ?? CreateNotificationsService(),
                    jobDefinitionsService ?? CreateJobDefinitionsService(),
                    resilliencePolicies ?? CreateResilliencePolicies(),
                    logger ?? CreateLogger()
                );
        }

        private IJobRepository CreateJobRepository()
        {
            return Substitute.For<IJobRepository>();
        }

        private INotificationService CreateNotificationsService()
        {
            return Substitute.For<INotificationService>();
        }

        private IJobDefinitionsService CreateJobDefinitionsService()
        {
            return Substitute.For<IJobDefinitionsService>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private IJobsResiliencePolicies CreateResilliencePolicies()
        {
            return JobsResilienceTestHelper.GenerateTestPolicies();
        }

    }
}
