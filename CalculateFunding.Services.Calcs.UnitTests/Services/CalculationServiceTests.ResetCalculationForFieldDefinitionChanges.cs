using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task ResetCalculationForFieldDefinitionChanges_GivenNoCalculationsToProcess_LogsAndDoesNotContinue()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = Enumerable.Empty<DatasetSpecificationRelationshipViewModel>();
            IEnumerable<string> currentFieldDefinitionNames = Enumerable.Empty<string>();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(Enumerable.Empty<Calculation>());

            CalculationService calculationService = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            await calculationService.ResetCalculationForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No calculations found to reset for specification id '{specificationId}'"));
        }

        [TestMethod]
        public async Task ResetCalculationForFieldDefinitionChanges_GivenNoCalculationsRequiredResetting_LogsAndDoesNotContinue()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = Enumerable.Empty<DatasetSpecificationRelationshipViewModel>();
            IEnumerable<string> currentFieldDefinitionNames = Enumerable.Empty<string>();

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                     Current = new CalculationVersion
                     {
                         SourceCode = "source"
                     }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            CalculationService calculationService = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            await calculationService.ResetCalculationForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No calculations required resetting for specification id '{specificationId}'"));
        }

        [TestMethod]
        public void ResetCalculationForFieldDefinitionChanges_GivenCalculationRequiresResetButUpdatingCalculationFails_ThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                new DatasetSpecificationRelationshipViewModel
                {
                     Name = "Test Name"
                }
            };

            IEnumerable<string> currentFieldDefinitionNames = new[]
            {
                "Test Field"
            };

            ILogger logger = CreateLogger();

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                     Current = new CalculationVersion
                     {
                         SourceCode = "return Datasets.TestName.TestField"
                     }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);
            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            CalculationService calculationService = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            Func<Task> test = async () =>await calculationService.ResetCalculationForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            test
                 .Should()
                 .ThrowExactly<InvalidOperationException>()
                 .Which
                 .Message
                 .Should()
                 .Be($"Update calculation returned status code 'BadRequest' instead of OK");
        }

        [TestMethod]
        public async Task ResetCalculationForFieldDefinitionChanges_GivenCalculationRequiresReset_UpdatesCalculationsAndDeletesAssembly()
        {
            //Arrange
            const string specificationId = "spec-id";

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                new DatasetSpecificationRelationshipViewModel
                {
                     Name = "Test Name"
                }
            };

            IEnumerable<string> currentFieldDefinitionNames = new[]
            {
                "Test Field"
            };

            ILogger logger = CreateLogger();

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "return Datasets.TestName.TestField",
                Date = DateTimeOffset.Now
            };

            Calculation calculation = new Calculation
            {
                Current = calculationVersion,
                SpecificationId = specificationId,
                FundingStreamId = "funding stream id",
            };
            
            IEnumerable<Calculation> calculations = new[]
            {
                calculation
            };

            BuildProject buildProject = new BuildProject();

            Build build = new Build
            {
                SourceFiles = new List<SourceFile>()
            };

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Test Spec Name",
                FundingStreams = new []
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);
            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Is(buildProject), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IVersionRepository<CalculationVersion> calculationVersionRepository = CreateCalculationVersionRepository();
            calculationVersionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            ICacheProvider cacheProvider = CreateCacheProvider();

            CalculationService calculationService = CreateCalculationService(
                logger: logger, 
                calculationsRepository: calculationsRepository, 
                specificationRepository: specificationRepository,
                buildProjectsService: buildProjectsService,
                sourceCodeService: sourceCodeService,
                calculationVersionRepository: calculationVersionRepository,
                cacheProvider: cacheProvider);

            //Act
            await calculationService.ResetCalculationForFieldDefinitionChanges(relationships, specificationId, currentFieldDefinitionNames);

            //Assert
            await
                sourceCodeService
                    .Received(1)
                    .DeleteAssembly(Arg.Is(specificationId));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<DatasetSchemaRelationshipModel>>(Arg.Is($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specificationId}"));
        }
    }
}
