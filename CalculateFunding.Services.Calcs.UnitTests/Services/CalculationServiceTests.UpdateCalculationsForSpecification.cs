using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void UpdateCalculationsForSpecification_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForSpecification(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasNoChanges_LogsAndReturns()
        {
            //Arrange
           SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Current = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } },
                Previous = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("No changes detected"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedFundingPeriodsButCalcculationsCouldNotBeFound_LogsAndReturns()
        {
            //Arrange
            const string specificationId = "spec-id";

           SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp2" } },
                Previous = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<Calculation>)null);

            CalculationService service = CreateCalculationService(calculationsRepository, logger);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No calculations found for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyName_SavesChangesEnsuresJobCreated()
        {
            // Arrange
            const string specificationId = "spec-id";

            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name"
                },
                Previous = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Id = "any-id",
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1,
                        Name = "any name",
                        CalculationType = CalculationType.Template,
                    }
                }
            };

            BuildProject buildProject = new BuildProject
            {
                Id = "build-project-1",
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1", JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob });

            IMapper mapper = Substitute.For<IMapper>();

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                             m.Properties["specification-id"] == specificationId &&
                             m.Trigger.EntityId == specificationId &&
                             m.Trigger.EntityType == "Specification" &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }
        
        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyNameAndGraphEnabled_SavesChangesEnsuresJobCreated()
        {
            // Arrange
            const string specificationId = "spec-id";

            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name"
                },
                Previous = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Id = "any-id",
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1,
                        Name = "any name",
                        CalculationType = CalculationType.Template,
                    }
                }
            };

            BuildProject buildProject = new BuildProject
            {
                Id = "build-project-1",
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(_ => _.JobDefinitionId == JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob))
                .Returns(new Job { Id = "job-id-1", JobDefinitionId = JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob });
            
            jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(_ => _.JobDefinitionId == JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob))
                .Returns(new Job { Id = "job-id-2", JobDefinitionId = JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob });

            IMapper mapper = Substitute.For<IMapper>();

            ICalculationsFeatureFlag calculationsFeatureFlag = CreateCalculationsFeatureFlag(true);

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                jobsApiClient: jobsApiClient,
                calculationsFeatureFlag: calculationsFeatureFlag);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob &&
                             m.Properties["specification-id"] == specificationId &&
                             m.Trigger.EntityId == specificationId &&
                             m.Trigger.EntityType == "Specification" &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob &&
                             m.Properties["specification-id"] == specificationId &&
                             m.Trigger.EntityId == specificationId &&
                             m.Trigger.EntityType == "Specification" &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob}' created with id: 'job-id-1'"));
        
            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob}' created with id: 'job-id-2'"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyNameButCreatingJobReturnsNull_LogsError()
        {
            // Arrange
            const string specificationId = "spec-id";

            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name"
                },
                Previous = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Id = "any-id",
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1,
                        Name = "any name",
                        CalculationType = CalculationType.Template
                    }
                }
            };

            BuildProject buildProject = new BuildProject
            {
                Id = "build-project-1",
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            IMapper mapper = Substitute.For<IMapper>();

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                jobsApiClient: jobsApiClient);

            // Act
            Func<Task> test = async () => await service.UpdateCalculationsForSpecification(message);

            // Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to create job: '{JobConstants.DefinitionNames.CreateInstructAllocationJob} for specification id '{specificationId}'");

            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                             m.Properties["specification-id"] == specificationId &&
                             m.Trigger.EntityId == specificationId &&
                             m.Trigger.EntityType == "Specification" &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to create job: '{JobConstants.DefinitionNames.CreateInstructAllocationJob} for specification id '{specificationId}'"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyNameAndSourceCodeContainsCalculationAggregate_SavesChangesEnsuresGenerateAggregationsJobCreated()
        {
            // Arrange
            const string specificationId = "spec-id";

            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name"
                },
                Previous = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Id = "any-id",
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "return Min(calc1)",
                        Version = 1,
                        Name = "any name",
                        CalculationType = CalculationType.Template
                    }
                }
            };

            BuildProject buildProject = new BuildProject
            {
                Id = "build-project-1",
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calcs);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1", JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob });

            IMapper mapper = Substitute.For<IMapper>();

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                jobsApiClient: jobsApiClient);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob &&
                             m.Properties["specification-id"] == specificationId &&
                             m.Trigger.EntityId == specificationId &&
                             m.Trigger.EntityType == "Specification" &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob}' created with id: 'job-id-1'"));
        }
    }
}
