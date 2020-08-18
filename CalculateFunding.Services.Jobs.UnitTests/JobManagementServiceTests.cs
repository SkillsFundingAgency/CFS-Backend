using CalculateFunding.Common.Caching;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    [TestClass]
    public partial class JobManagementServiceTests
    {
        const string jobDefinitionId = "JobDefinition";
        const string jobDefinitionIdTwo = "JobDefinitionTwo";

        public JobManagementService CreateJobManagementService(
            IJobRepository jobRepository = null,
            INotificationService notificationService = null,
            IJobDefinitionsService jobDefinitionsService = null,
            IJobsResiliencePolicies resiliencePolicies = null,
            ILogger logger = null,
            IValidator<CreateJobValidationModel> createJobValidator = null,
            IMessengerService messengerService = null,
            ICacheProvider cacheProvider = null)
        {
            return new JobManagementService(
                    jobRepository ?? CreateJobRepository(),
                    notificationService ?? CreateNotificationsService(),
                    jobDefinitionsService ?? CreateJobDefinitionsService(),
                    resiliencePolicies ?? CreateResiliencePolicies(),
                    logger ?? CreateLogger(),
                    createJobValidator ?? CreateNewCreateJobValidator(),
                    messengerService ?? CreateMessengerService(),
                    cacheProvider ?? CreateCacheProvider()
                );
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
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

        private IJobsResiliencePolicies CreateResiliencePolicies()
        {
            return JobsResilienceTestHelper.GenerateTestPolicies();
        }

        private static IValidator<CreateJobValidationModel> CreateNewCreateJobValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CreateJobValidationModel> validator = Substitute.For<IValidator<CreateJobValidationModel>>();

            validator
               .Validate(Arg.Any<CreateJobValidationModel>())
               .Returns(validationResult);

            return validator;
        }

        private static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }
    }
}
