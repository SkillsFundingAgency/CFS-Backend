using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
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
            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Current = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
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

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp2" } },
                Previous = new Models.Specs.SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
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
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyName_SavesChanges()
        {
            // Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fp1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>{ new Reference { Id = "pol-id", Name = "policy1"} }
                }
            };

            BuildProject buildProject = null;

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
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsService: buildProjectsService, searchRepository: searchRepository, jobsApiClient: jobsApiClient);

            // Act
            await service.UpdateCalculationsForSpecification(message);

            // Assert
            calcs
                .First()
                .Policies
                .First()
                .Name
                .Should()
                .Be("policy2");

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.First().PolicySpecificationNames.Contains("policy2")));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedFundingStreams_SetsTheAllocationLineAndFundingStreamToNull()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    FundingStreams = new List<Reference> { new Reference { Id = "fs2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    FundingStreams = new List<Reference> { new Reference { Id = "fs1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calcs = new[]
            {
                new Calculation
                {
                    SpecificationId =  "spec-id",
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fs1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>()
                }
            };

            BuildProject buildProject = new BuildProject();

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
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(calculationsRepository, logger, buildProjectsService: buildProjectsService, searchRepository: searchRepository, jobsApiClient: jobsApiClient);

            //Act
            await service.UpdateCalculationsForSpecification(message);

            //Assert
            calcs
                .First()
                .FundingStream
                .Should()
                .BeNull();

            calcs
               .First()
               .AllocationLine
               .Should()
               .BeNull();

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(c =>
                    c.First().Id == calcs.First().Id &&
                    c.First().FundingStreamId == "" &&
                    c.First().FundingStreamName == "No funding stream set"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyName_SavesChangesEnsuresJobCreated()
        {
            // Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
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
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fp1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>{ new Reference { Id = "pol-id", Name = "policy1"} }
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
                             m.Trigger.EntityType == nameof(Models.Specs.Specification) &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }

        [TestMethod]
        public async Task UpdateCalculationsForSpecification_GivenModelHasChangedPolicyNameButCreatingJobReturnsNull_LogsError()
        {
            // Arrange
            const string specificationId = "spec-id";

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
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
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fp1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "source code",
                        Version = 1
                    },
                    Policies = new List<Reference>{ new Reference { Id = "pol-id", Name = "policy1"} }
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
                             m.Trigger.EntityType == nameof(Models.Specs.Specification) &&
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

            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
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
                    Name = "any name",
                    Id = "any-id",
                    CalculationSpecification = new Reference("any name", "any-id"),
                    FundingPeriod = new Reference("18/19", "2018/2019"),
                    CalculationType = CalculationType.Number,
                    FundingStream = new Reference("fp1","fs1-111"),
                    Current = new CalculationVersion
                    {
                        Author = new Reference(UserId, Username),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        SourceCode = "return Min(calc1)",
                        Version = 1
                    },
                    Policies = new List<Reference>{ new Reference { Id = "pol-id", Name = "policy1"} }
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
                             m.Trigger.EntityType == nameof(Models.Specs.Specification) &&
                             m.Trigger.Message == $"Updating calculations for specification: '{specificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob}' created with id: 'job-id-1'"));
        }
    }
}
