using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Jobs;
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

        public JobManagementService CreateJobManagementService(
            IJobRepository jobRepository = null,
            INotificationService notificationService = null,
            IJobDefinitionsService jobDefinitionsService = null,
            IJobsResiliencePolicies resilliencePolicies = null,
            ILogger logger = null,
            IValidator<CreateJobValidationModel> createJobValidator = null,
            IMessengerService messengerService = null)
        {
            return new JobManagementService(
                    jobRepository ?? CreateJobRepository(),
                    notificationService ?? CreateNotificationsService(),
                    jobDefinitionsService ?? CreateJobDefinitionsService(),
                    resilliencePolicies ?? CreateResilliencePolicies(),
                    logger ?? CreateLogger(),
                    createJobValidator ?? CreateNewCreateJobValidator(),
                    messengerService ?? CreateMessengerService()
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
