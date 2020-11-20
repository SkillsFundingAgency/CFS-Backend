using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task UpdateTemplateCalculationsForSpecification_GivenNoTemplateCalculationsForSpecification_ReturnsNotFoundObjectResults()
        {
            // Arrange
            string datasetRelationshipId = NewRandomString();
            Reference user = new Reference(NewRandomString(), NewRandomString());

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetTemplateCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(new List<Calculation>());

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger);

            // Act
            IActionResult result = await service.UpdateTemplateCalculationsForSpecification(SpecificationId, datasetRelationshipId, user);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No template calculations found for specification id '{SpecificationId}'");
        }

        [TestMethod]
        public async Task UpdateTemplateCalculationsForSpecification_GivenNoDatasetRelationshipFoundForRelationId_ReturnsNotFoundObjectResults()
        {
            // Arrange
            string datasetRelationshipId = NewRandomString();
            string datasetDefinitionId = NewRandomString();
            Reference user = new Reference(NewRandomString(), NewRandomString());

            string calcName1 = NewRandomString();
            string calcName2 = NewRandomString();

            Calculation calculation1 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName1,
                    CalculationType = CalculationType.Template,
                }
            };
            Calculation calculation2 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName2,
                    CalculationType = CalculationType.Template,
                }
            };

            IEnumerable<Calculation> calcs = new[] { calculation1, calculation2 };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetTemplateCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calcs);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient.GetDataSourcesByRelationshipId(Arg.Is(datasetRelationshipId))
                .Returns(new ApiResponse<SelectDatasourceModel>(HttpStatusCode.NotFound));

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                datasetsApiClient: datasetsApiClient);

            // Act
            IActionResult result = await service.UpdateTemplateCalculationsForSpecification(SpecificationId, datasetRelationshipId, user);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No dataset relationship found for dataset relationship id '{datasetRelationshipId}'");
        }

        [TestMethod]
        public async Task UpdateTemplateCalculationsForSpecification_GivenNoDatasetDefinitionFoundForRelationId_ReturnsNotFoundObjectResults()
        {
            // Arrange
            string datasetRelationshipId = NewRandomString();
            string datasetRelationshipName = NewRandomString();
            string datasetDefinitionId = NewRandomString();
            Reference user = new Reference(NewRandomString(), NewRandomString());

            string calcName1 = NewRandomString();
            string calcName2 = NewRandomString();

            Calculation calculation1 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName1,
                    CalculationType = CalculationType.Template,
                }
            };
            Calculation calculation2 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName2,
                    CalculationType = CalculationType.Template,
                }
            };

            IEnumerable<Calculation> calcs = new[] { calculation1, calculation2 };

            SelectDatasourceModel datasetRelationship = new SelectDatasourceModel()
            {
                DefinitionId = datasetDefinitionId,
                RelationshipName = datasetRelationshipName,
                RelationshipId = datasetRelationshipId
            };

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                Id = SpecificationId,
                FundingStreams = new[] { new Reference(FundingStreamId, FundingStreamId) }
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetTemplateCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calcs);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient.GetDataSourcesByRelationshipId(Arg.Is(datasetRelationshipId))
                .Returns(new ApiResponse<SelectDatasourceModel>(HttpStatusCode.OK, datasetRelationship));

            datasetsApiClient.GetDatasetDefinitionById(Arg.Is(datasetDefinitionId))
                .Returns(new ApiResponse<DatasetDefinition>(HttpStatusCode.NotFound));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient.GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                specificationsApiClient: specificationsApiClient,
                datasetsApiClient: datasetsApiClient);

            // Act
            IActionResult result = await service.UpdateTemplateCalculationsForSpecification(SpecificationId, datasetRelationshipId, user);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No dataset definition found for dataset definition id '{datasetDefinitionId}'");
        }

        [TestMethod]
        public async Task UpdateTemplateCalculationsForSpecification_GivenSpecificationAndRelationId_ShouldUpdateCalucationSourceCodeAndApprove()
        {
            // Arrange
            string datasetRelationshipId = NewRandomString();
            string datasetRelationshipName = NewRandomString();
            string datasetDefinitionId = NewRandomString();
            Reference user = new Reference(NewRandomString(), NewRandomString());

            string calcName1 = NewRandomString();
            string calcName2 = NewRandomString();
            string calculation1Id = NewRandomString();
            string calculation2Id = NewRandomString();

            string datasetRelationshipIdVisualBasicVariable = VisualBasicTypeGenerator.GenerateIdentifier(datasetRelationshipName);

            string expectedCalc1Code = @$"If Datasets.{datasetRelationshipIdVisualBasicVariable}.HasValue = False Then Return Nothing

Return Datasets.{datasetRelationshipIdVisualBasicVariable}.{VisualBasicTypeGenerator.GenerateIdentifier(calcName1)}";

            string expectedCalc2Code = @$"If Datasets.{datasetRelationshipIdVisualBasicVariable}.HasValue = False Then Return Nothing

Return Datasets.{datasetRelationshipIdVisualBasicVariable}.{VisualBasicTypeGenerator.GenerateIdentifier(calcName2)}";


            Calculation calculation1 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = calculation1Id,
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName1,
                    CalculationType = CalculationType.Template,
                    CalculationId = calculation1Id,
                }
            };
            Calculation calculation2 = new Calculation
            {
                SpecificationId = SpecificationId,
                Id = calculation2Id,
                Current = new CalculationVersion
                {
                    Author = user,
                    Date = DateTimeOffset.Now,
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    SourceCode = NewRandomString(),
                    Version = 1,
                    Name = calcName2,
                    CalculationType = CalculationType.Template,
                    CalculationId = calculation2Id
                }
            };

            CalculationVersion existingCalculationVersion1 = calculation1.Current.Clone() as CalculationVersion;
            existingCalculationVersion1.SourceCode = "Return Nothing";
            existingCalculationVersion1.PublishStatus = Models.Versioning.PublishStatus.Approved;

            CalculationVersion existingCalculationVersion2 = calculation2.Current.Clone() as CalculationVersion;
            existingCalculationVersion2.SourceCode = "Return Nothing";
            existingCalculationVersion2.PublishStatus = Models.Versioning.PublishStatus.Approved;

            IEnumerable<Calculation> calcs = new[] { calculation1, calculation2 };

            SelectDatasourceModel datasetRelationship = new SelectDatasourceModel()
            {
                DefinitionId = datasetDefinitionId,
                RelationshipName = datasetRelationshipName,
                RelationshipId = datasetRelationshipId
            };
            DatasetDefinition datasetDefinition = new DatasetDefinition()
            {
                Id = datasetDefinitionId,
                TableDefinitions = new List<TableDefinition> {
                    new TableDefinition()
                    {
                        FieldDefinitions = new List<FieldDefinition>
                        {
                            new FieldDefinition() { Name = calcName1 },
                            new FieldDefinition() { Name = calcName2 }
                        }
                    }
                }
            };

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                Id = SpecificationId,
                FundingStreams = new[] { new Reference(FundingStreamId, FundingStreamId) }
            };

            BuildProject buildProject = new BuildProject
            {
                Id = NewRandomString(),
                SpecificationId = SpecificationId,
                Build = new Build() { SourceFiles = new List<SourceFile>() }
            };

            IEnumerable<string> calculationNames = new[] { calcName1, calcName2 };
            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetTemplateCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calcs);
            calculationsRepository.UpdateCalculation(Arg.Is<Calculation>(c => calculationNames.Any(x => x == c.Name)))
                .Returns(HttpStatusCode.OK);

            calculationsRepository.GetCompilerOptions(Arg.Is(SpecificationId)).Returns(new CompilerOptions());

            IVersionRepository<CalculationVersion> calculationVersionRepository = CreateCalculationVersionRepository();


            // NOTE: This test is returning the existing version, rather than the new version of the calculation, which means the asserts aren't against the latest version / generated code
            // The c.SourceCode check here ensures the expected source code is persisted. Non matching will be asserted below with an HTTP Status Code of 0 for not found

            calculationVersionRepository.CreateVersion(Arg.Is<CalculationVersion>(c => c.Name == calcName1 && c.SourceCode == expectedCalc1Code), Arg.Is<CalculationVersion>(c => c.Name == calcName1), null, false)
            .Returns(existingCalculationVersion1);

            calculationVersionRepository.CreateVersion(Arg.Is<CalculationVersion>(c => c.Name == calcName2 && c.SourceCode == expectedCalc2Code), Arg.Is<CalculationVersion>(c => c.Name == calcName2), null, false)
            .Returns(existingCalculationVersion2);

            calculationVersionRepository.SaveVersion(Arg.Is<CalculationVersion>(c => calculationNames.Any(x => x == c.Name)))
             .Returns(HttpStatusCode.OK);

            IDatasetsApiClient datasetsApiClient = CreateDatasetsApiClient();
            datasetsApiClient.GetDataSourcesByRelationshipId(Arg.Is(datasetRelationshipId))
                .Returns(new ApiResponse<SelectDatasourceModel>(HttpStatusCode.OK, datasetRelationship));

            datasetsApiClient.GetDatasetDefinitionById(Arg.Is(datasetDefinitionId))
                .Returns(new ApiResponse<DatasetDefinition>(HttpStatusCode.OK, datasetDefinition));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient.GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(SpecificationId))
                .Returns(Task.FromResult(buildProject));

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService.Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(new Build() { SourceFiles = new List<SourceFile>() });

            CalculationService service = CreateCalculationService(
                calculationsRepository,
                logger,
                buildProjectsService: buildProjectsService,
                specificationsApiClient: specificationsApiClient,
                datasetsApiClient: datasetsApiClient,
                calculationVersionRepository: calculationVersionRepository,
                sourceCodeService: sourceCodeService);

            // Act
            IActionResult result = await service.UpdateTemplateCalculationsForSpecification(SpecificationId, datasetRelationshipId, user);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();



            await calculationVersionRepository
                .Received()
                .SaveVersion(Arg.Is<CalculationVersion>(v => v.Name == calcName1 && v.PublishStatus == Models.Versioning.PublishStatus.Approved));

            await calculationVersionRepository
                .Received()
                .SaveVersion(Arg.Is<CalculationVersion>(v => v.Name == calcName1 && v.PublishStatus == Models.Versioning.PublishStatus.Approved));

            await calculationsRepository
                .Received()
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Id == calculation1Id));

            await calculationsRepository
               .Received()
               .UpdateCalculation(Arg.Is<Calculation>(c => c.Id == calculation2Id));

        }

        public string NewRandomString() => new RandomString();
    }
}
