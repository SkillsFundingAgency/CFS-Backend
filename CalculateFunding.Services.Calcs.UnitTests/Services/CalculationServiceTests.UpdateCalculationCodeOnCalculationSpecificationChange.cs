using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenCalculationsFoundReferencingCalculationToBeUpdated_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                buildProjectsService: buildProjectsService);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";
            const string originalCodeUpdate = @"Dim test as OriginalNameOptions? 
                                                OriginalNameOptions.enumName 
                                                Return Calculations.OriginalName()";
            const string newCodeUpdated = @"Dim test as CalculationToUpdateOptions? 
                                                CalculationToUpdateOptions.enumName 
                                                Return Calculations.CalculationToUpdate()";
            const string originalCodeIgnore = "Return 10";
            const string fundingStreamId = "fundingstreamid";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                CurrentName = "Calculation To Update",
                PreviousName = "Original Name",
                SpecificationId = specificationId,
                CalculationDataType = CalculationDataType.Enum,
                Namespace = "Calculations"
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>
            {
                new Calculation
                {
                    Id = calculationId,
                    SpecificationId = specificationId,
                    FundingStreamId = fundingStreamId,
                    Current = new CalculationVersion
                    {
                        SourceCode = originalCodeIgnore,
                        Name = "Calculation to Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description",
                        DataType = CalculationDataType.Enum
                    }
                },
                new Calculation
                {
                    Id = "referenceCalc",
                    SpecificationId = specificationId,
                    FundingStreamId = fundingStreamId,
                    Current = new CalculationVersion
                    {
                        SourceCode = originalCodeUpdate,
                        Name = "Calling Calculation To Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description"
                    }
                }
            };

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
                FundingStreams = new []{new Reference(fundingStreamId, "fundingStreamName"), }
            };

            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = newCodeUpdated,
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Is<CalculationVersion>(_ => _.SourceCode == newCodeUpdated), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            Calculation calculation = updatedCalculations.Single();

            calculation.Current.SourceCode
                .Should()
                .Be(newCodeUpdated);


            calculation.Current.Version
                .Should()
                .Be(2);

            calculation.Id
                .Should()
                .Be("referenceCalc");

            await buildProjectsService
                .DidNotReceive()
                .GetBuildProjectForSpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenCalculationsFoundReferencingCalculationToBeUpdatedHasDifferentNameCasing_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                buildProjectsService: buildProjectsService);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";
            const string fundingStreamId = "fundingStreamId";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                CurrentName = "Calculation To Update",
                PreviousName = "Original Name",
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            const string originalCodeIgnore = "Return 10";
            const string originalCodeUpdate = "Return Calculations.OriginalName()";

            List<Calculation> calculations = new List<Calculation>
            {
                new Calculation
                {
                    Id = calculationId,
                    SpecificationId = specificationId,
                    FundingStreamId = fundingStreamId,
                    Current = new CalculationVersion
                    {
                        SourceCode = originalCodeIgnore,
                        Name = "Calculation to Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description"
                    }
                },
                new Calculation()
                {
                    Id = "referenceCalc",
                    SpecificationId = specificationId,
                    FundingStreamId = fundingStreamId,
                    Current = new CalculationVersion
                    {
                        SourceCode = originalCodeUpdate,
                        Name = "Calling Calculation To Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description",
                    }
                }
            };

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
                FundingStreams = new []  { new Reference(fundingStreamId, "funding stream name"),  }
            };

            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "Return CalculationToUpdate()",
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(1);

            updatedCalculations
                .First()
                .Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");

            updatedCalculations
                .First()
                .Current.Version
                .Should()
                .Be(2);

            updatedCalculations
                .First()
                .Id
                .Should()
                .Be("referenceCalc");

            await buildProjectsService
                .DidNotReceive()
                .GetBuildProjectForSpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenNoCalculationsFoundReferencingCalculationToBeUpdated_ThenNoCalculationsUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                CurrentName = "Calculation To Update",
                PreviousName = "Original Name",
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>()
            {
                new Calculation
                {
                    Id = calculationId,
                    SpecificationId = specificationId,
                    Current = new CalculationVersion
                    {
                        SourceCode = "Return 10",
                        Name = "Calculation to Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description"
                    }
                },
                new Calculation
                {
                    Id = "referenceCalc",
                    SpecificationId = specificationId,
                    Current = new CalculationVersion
                    {
                        SourceCode = "Return 50",
                        Name = "Calling Calculation To Update",
                        CalculationType = CalculationType.Template,
                        Description = "Calculation Description",
                    }
                }
            };

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationById(Arg.Is(calculations[0].Id))
                .Returns(calculations[0]);

            SpecModel.SpecificationSummary specification = new SpecModel.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specification));

            CalculationVersion calculationVersion = new CalculationVersion
            {
                SourceCode = "Return CalculationToUpdate()",
                Version = 2
            };

            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            // Act
            IEnumerable<Calculation> updatedCalculations = await service.UpdateCalculationCodeOnCalculationChange(comparison, user);

            // Assert
            updatedCalculations
                .Should()
                .HaveCount(0);
        }
    }
}
