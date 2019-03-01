using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Reference = CalculateFunding.Common.Models.Reference;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenModelButCurrentIsNull_LogsDoesNotSave()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel();

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenModelButPreviousIsNull_LogsDoesNotSave()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation()
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CalculationService service = CreateCalculationService();

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .Should().ThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenNoChanges_LogsAndReturns()
        {
            //Arrange
            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                CalculationId = "calc-id",
                SpecificationId = "spec-id",
                Current = new Models.Specs.Calculation(),
                Previous = new Models.Specs.Calculation()
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            logger
                .Received(1)
                .Information("No changes detected for calculation with id: '{calculationId}' on specification '{specificationId}'", Arg.Is("calc-id"), Arg.Is("spec-id"));
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenChangesButSpecificationCouldNotBeFound_ThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id";

            Models.Specs.CalculationVersionComparisonModel model = new Models.Specs.CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "new name"
                },
                Previous = new Models.Specs.Calculation(),
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .Should().ThrowExactly<Exception>()
              .Which
              .Message
              .Should()
              .Be($"Specification could not be found for specification id : {specificationId}");
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenCalculationNotInCosmos_ThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Models.Calcs.Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, specificationRepository: specificationRepository, calculationsRepository: calculationsRepository);

            //Act
            Func<Task> test = async () => await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            test
              .Should().ThrowExactly<Exception>()
              .Which
              .Message
              .Should()
              .Be($"Calculation could not be found for calculation id : {CalculationId}");
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenCalcTypeChangedToNumber_RemovesAllocationLine()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                CalculationSpecification = new Reference
                {
                    Id = "calc-spec-id",
                    Name = "calc spec name"
                },
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger, 
                specificationRepository: specificationRepository, 
                calculationsRepository: calculationsRepository,
                sourceCodeService: sourceCodeService);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            specCalculation
                .CalculationType
                .Should()
                .Be(Models.Calcs.CalculationType.Number);

            specCalculation
               .AllocationLine
               .Should()
               .BeNull();
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenChanges_UpdatesCosmosAndSearch()
        {
            //Arrange
            const string specificationId = "spec-id";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name"
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(build);

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(logger: logger,
                specificationRepository: specificationRepository, calculationsRepository: calculationsRepository, 
                searchRepository: searchRepository, sourceCodeService: sourceCodeService);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            await
                calculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(m => m.Count() == 1));

            await
                calculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(
                    m => m.First().Name == "name" &&
                    m.First().AllocationLine == null
               ));

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.Count() == 1));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 1), Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenAllocationLineHasChangedButNoBuildProject_UpdatesCosmosWithNewFundingStreamCreatesBuildProject()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string allocationLineIdForFs1 = "AllocLineFS1";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId,
            };

            FundingStream expectedFundingStream = new FundingStream()
            {
                Name = "FundingStream1",
                Id = "FS1",
                AllocationLines = new List<AllocationLine>()
                {
                    new AllocationLine()
                    {
                        Id = allocationLineIdForFs1
                    }
                }
            };
            IEnumerable<FundingStream> fundingStreamsToReturn = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS2",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS2"
                        }
                    }
                },
                expectedFundingStream,
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS3",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS3"
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger mockLogger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = fundingStreamsToReturn
            };

            ISpecificationRepository mockSpecificationRepository = CreateSpecificationRepository();

            mockSpecificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            mockSpecificationRepository
                .GetFundingStreams()
                .Returns(fundingStreamsToReturn);

            ICalculationsRepository mockCalculationsRepository = CreateCalculationsRepository();
            mockCalculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> mockSearchRepository = CreateSearchRepository();

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(build);

            CalculationService service =
                CreateCalculationService(logger: mockLogger,
                    specificationRepository: mockSpecificationRepository,
                    calculationsRepository: mockCalculationsRepository,
                    searchRepository: mockSearchRepository,
                    buildProjectsRepository: buildProjectsRepository,
                    sourceCodeService: sourceCodeService);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            await
                mockCalculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(m => m.Count() == 1));

            await
                mockCalculationsRepository
                    .Received(1)
                    .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(
                        m => m.First().Name == "name" &&
                             m.First().AllocationLine.Id == allocationLineIdForFs1 &&
                             m.First().FundingStream.Name == expectedFundingStream.Name &&
                             m.First().FundingStream.Id == expectedFundingStream.Id
                    ));

            await
                mockSearchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.Count() == 1));

            await
                buildProjectsRepository
                .Received(1)
                .CreateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenAllocationLineHasChangedAndBuildProjectExists_UpdatesCosmosWithNewFundingStreamUpdatesBuildProject()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string allocationLineIdForFs1 = "AllocLineFS1";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = "1" },
                    CalculationType = Models.Specs.CalculationType.Number
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId,
            };

            FundingStream expectedFundingStream = new FundingStream()
            {
                Name = "FundingStream1",
                Id = "FS1",
                AllocationLines = new List<AllocationLine>()
                {
                    new AllocationLine()
                    {
                        Id = allocationLineIdForFs1
                    }
                }
            };
            IEnumerable<FundingStream> fundingStreamsToReturn = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS2",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS2"
                        }
                    }
                },
                expectedFundingStream,
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS3",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS3"
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger mockLogger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = fundingStreamsToReturn
            };

            BuildProject buildProject = new BuildProject();

            ISpecificationRepository mockSpecificationRepository = CreateSpecificationRepository();

            mockSpecificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            mockSpecificationRepository
                .GetFundingStreams()
                .Returns(fundingStreamsToReturn);

            ICalculationsRepository mockCalculationsRepository = CreateCalculationsRepository();
            mockCalculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> mockSearchRepository = CreateSearchRepository();

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile(), new SourceFile()}
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(build);

            CalculationService service =
                CreateCalculationService(logger: mockLogger,
                    specificationRepository: mockSpecificationRepository,
                    calculationsRepository: mockCalculationsRepository,
                    searchRepository: mockSearchRepository,
                    buildProjectsRepository: buildProjectsRepository,
                    sourceCodeService: sourceCodeService);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            await
                mockCalculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(m => m.Count() == 1));

            await
                mockCalculationsRepository
                    .Received(1)
                    .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(
                        m => m.First().Name == "name" &&
                             m.First().AllocationLine.Id == allocationLineIdForFs1 &&
                             m.First().FundingStream.Name == expectedFundingStream.Name &&
                             m.First().FundingStream.Id == expectedFundingStream.Id
                    ));

            await
                mockSearchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.Count() == 1));

            await
                buildProjectsRepository
                .Received(1)
                .UpdateBuildProject(Arg.Is(buildProject));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 2), Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenAllocationLineHasNotChangedButFundingStreamIsNull_UpdatesCosmosWithFundingStream()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string allocationLineIdForFs1 = "AllocLineFS1";
            const string calculationNewName = "newname";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = calculationNewName,
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId
            };

            FundingStream expectedFundingStream = new FundingStream()
            {
                Name = "FundingStream1",
                Id = "FS1",
                AllocationLines = new List<AllocationLine>()
                {
                    new AllocationLine()
                    {
                        Id = allocationLineIdForFs1
                    }
                }
            };
            IEnumerable<FundingStream> fundingStreamsToReturn = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS2",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS2"
                        }
                    }
                },
                expectedFundingStream,
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS3",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS3"
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger mockLogger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = fundingStreamsToReturn
            };

            ISpecificationRepository mockSpecificationRepository = CreateSpecificationRepository();

            mockSpecificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository mockCalculationsRepository = CreateCalculationsRepository();
            mockCalculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> mockSearchRepository = CreateSearchRepository();

            mockSpecificationRepository
                .GetFundingStreams()
                .Returns(fundingStreamsToReturn);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile(), new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(build);

            CalculationService service =
                CreateCalculationService(logger: mockLogger,
                    specificationRepository: mockSpecificationRepository,
                    calculationsRepository: mockCalculationsRepository,
                    searchRepository: mockSearchRepository,
                    sourceCodeService: sourceCodeService);

            //Act
            await service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            await
                mockCalculationsRepository
                .Received(1)
                .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(m => m.Count() == 1));

            await
                mockCalculationsRepository
                    .Received(1)
                    .UpdateCalculations(Arg.Is<IEnumerable<Models.Calcs.Calculation>>(
                        m => m.First().Name == calculationNewName &&
                             m.First().AllocationLine.Id == allocationLineIdForFs1 &&
                             m.First().FundingStream.Name == expectedFundingStream.Name &&
                             m.First().FundingStream.Id == expectedFundingStream.Id
                    ));

            await
                mockSearchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<CalculationIndex>>(m => m.Count() == 1));
        }

        [TestMethod]
        public void UpdateCalculationsForCalculationSpecificationChange_GivenAllocationLineIdHasNoMatchInSystem_ShouldThrowException()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string allocationLineIdForFs1 = "AllocLineFS1";
            const string calculationNewName = "newname";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = calculationNewName,
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId
            };
            IEnumerable<FundingStream> fundingStreamsToReturn = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS2",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS2"
                        }
                    }
                },
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS3",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS3"
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger mockLogger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = fundingStreamsToReturn
            };

            ISpecificationRepository mockSpecificationRepository = CreateSpecificationRepository();
            mockSpecificationRepository
                .GetFundingStreams()
                .Returns(fundingStreamsToReturn);

            mockSpecificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository mockCalculationsRepository = CreateCalculationsRepository();
            mockCalculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> mockSearchRepository = CreateSearchRepository();

            CalculationService service =
                CreateCalculationService(logger: mockLogger,
                    specificationRepository: mockSpecificationRepository,
                    calculationsRepository: mockCalculationsRepository,
                    searchRepository: mockSearchRepository);

            //Act
            Func<Task> updateCalculationsFunction = () => service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            Assert.ThrowsExceptionAsync<InvalidOperationException>(updateCalculationsFunction);
        }

        [TestMethod]
        public async Task UpdateCalculationsForCalculationSpecificationChange_GivenASpecificationHasNoFundingStreamsButAnAllocationLineHasChanged_ShouldThrowException()
        {
            //Arrange
            const string specificationId = "spec-id";
            const string allocationLineIdForFs1 = "AllocLineFS1";
            const string calculationNewName = "newname";

            CalculationVersionComparisonModel model = new CalculationVersionComparisonModel
            {
                Current = new Models.Specs.Calculation
                {
                    Name = calculationNewName,
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                Previous = new Models.Specs.Calculation
                {
                    Name = "name",
                    AllocationLine = new Reference { Id = allocationLineIdForFs1 },
                    CalculationType = Models.Specs.CalculationType.Funding
                },
                CalculationId = CalculationId,
                SpecificationId = specificationId,
            };

            Models.Calcs.Calculation specCalculation = new Models.Calcs.Calculation
            {
                Name = "name",
                AllocationLine = new Reference { Id = "1" },
                CalculationType = Models.Calcs.CalculationType.Funding,
                Id = CalculationId,
                FundingPeriod = new Reference { Id = "fp1", Name = "fp 1" },
                Policies = new List<Reference> { new Reference { Id = "pol1", Name = "pol2" } },
                Current = new CalculationVersion
                {
                    SourceCode = "source code",
                    PublishStatus = PublishStatus.Approved
                },
                CalculationSpecification = new Reference { Id = CalculationId, Name = "name" },
                SpecificationId = specificationId
            };
            IEnumerable<FundingStream> fundingStreamsToReturn = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS2",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS2"
                        }
                    }
                },
                new FundingStream()
                {
                    Name = "FundingStream2",
                    Id = "FS3",
                    AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id = "AllocLineFS3"
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(model);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger mockLogger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Name = "spec name",
                FundingStreams = fundingStreamsToReturn
            };

            ISpecificationRepository mockSpecificationRepository = CreateSpecificationRepository();
            mockSpecificationRepository
                .GetFundingStreams()
                .Returns(fundingStreamsToReturn);

            mockSpecificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            ICalculationsRepository mockCalculationsRepository = CreateCalculationsRepository();
            mockCalculationsRepository
                .GetCalculationByCalculationSpecificationId(Arg.Is(CalculationId))
                .Returns(specCalculation);

            ISearchRepository<CalculationIndex> mockSearchRepository = CreateSearchRepository();

            CalculationService service =
                CreateCalculationService(logger: mockLogger,
                    specificationRepository: mockSpecificationRepository,
                    calculationsRepository: mockCalculationsRepository,
                    searchRepository: mockSearchRepository);

            //Act
            Func<Task> updateCalculationsFunction = () => service.UpdateCalculationsForCalculationSpecificationChange(message);

            //Assert
            Assert.ThrowsExceptionAsync<InvalidOperationException>(updateCalculationsFunction);
        }
    }
}