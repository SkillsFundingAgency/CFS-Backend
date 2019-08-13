using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        private ICalculationCodeReferenceUpdate FakeCalculationCodeReferenceUpdate()
        {
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = CreateCalculationCodeReferenceUpdate();
            calculationCodeReferenceUpdate
                .ReplaceSourceCodeReferences(Arg.Any<Calculation>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(x =>
                {
                    string source = x.ArgAt<Calculation>(0).Current.SourceCode;
                    string oldName = x.ArgAt<string>(1);
                    string newName = x.ArgAt<string>(2);

                    return source.Replace(oldName, newName);
                });
            return calculationCodeReferenceUpdate;
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenCalculationsFoundReferencingCalculationToBeUpdated_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = FakeCalculationCodeReferenceUpdate();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                buildProjectsService: buildProjectsService,
                calculationCodeReferenceUpdate: calculationCodeReferenceUpdate);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";
            const string originalCodeUpdate = "Return OriginalName()";
            const string originalCodeIgnore = "Return 10";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Calculation To Update"
                    }
                },
                Previous = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Original Name"
                    }
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            List<Calculation> calculations = new List<Calculation>
            {
                new Calculation
                {
                    Id = calculationId,
                    SpecificationId = specificationId,
                    Current = new CalculationVersion
                    {
                        SourceCode = originalCodeIgnore,
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

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

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

            Calculation calculation = updatedCalculations.Single();

            calculation.Current.SourceCode
                .Should()
                .Be("Return CalculationToUpdate()");


            calculation.Current.Version
                .Should()
                .Be(2);

            calculation.Id
                .Should()
                .Be("referenceCalc");

            await buildProjectsService
                .DidNotReceive()
                .GetBuildProjectForSpecificationId(Arg.Any<string>());

            calculationCodeReferenceUpdate
                .Received(calculations.Count)
                .ReplaceSourceCodeReferences(Arg.Any<Calculation>(),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name));

            calculationCodeReferenceUpdate
                .Received(1)
                .ReplaceSourceCodeReferences(Arg.Is<Calculation>(c => c.Current.SourceCode == originalCodeIgnore), Arg.Any<string>(), Arg.Any<string>());

            calculationCodeReferenceUpdate
                .Received(1)
                .ReplaceSourceCodeReferences(Arg.Is<Calculation>(c => c.Current.SourceCode == originalCodeIgnore), Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenCalculationsFoundReferencingCalculationToBeUpdatedHasDifferentNameCasing_ThenSourceCodeUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = FakeCalculationCodeReferenceUpdate();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                buildProjectsService: buildProjectsService,
                calculationCodeReferenceUpdate: calculationCodeReferenceUpdate);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Calculation To Update"
                    }
                },
                Previous = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Original Name"
                    }
                },
                SpecificationId = specificationId,
            };

            Reference user = new Reference("userId", "User Name");

            const string originalCodeIgnore = "Return 10";
            const string originalCodeUpdate = "Return OriginalName()";

            List<Calculation> calculations = new List<Calculation>
            {
                new Calculation
                {
                    Id = calculationId,
                    SpecificationId = specificationId,
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

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

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

            calculationCodeReferenceUpdate
                .Received(calculations.Count)
                .ReplaceSourceCodeReferences(Arg.Any<Calculation>(),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name));

            calculationCodeReferenceUpdate
                .Received(1)
                .ReplaceSourceCodeReferences(Arg.Is<Calculation>(c => c.Current.SourceCode == originalCodeIgnore), Arg.Any<string>(), Arg.Any<string>());

            calculationCodeReferenceUpdate
                .Received(1)
                .ReplaceSourceCodeReferences(Arg.Is<Calculation>(c => c.Current.SourceCode == originalCodeIgnore), Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateCalculationCodeOnCalculationChange_WhenNoCalculationsFoundReferencingCalculationToBeUpdated_ThenNoCalculationsUpdated()
        {
            // Arrange
            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate = FakeCalculationCodeReferenceUpdate();

            CalculationService service = CreateCalculationService(calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                calculationCodeReferenceUpdate: calculationCodeReferenceUpdate);

            const string specificationId = "specId";
            const string calculationId = "updatedCalc";

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = calculationId,
                Current = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Calculation To Update"
                    }
                },
                Previous = new Calculation
                {
                    Id = "calcSpec1",
                    Current = new CalculationVersion
                    {
                        Name = "Original Name"
                    }
                },
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

            Models.Specs.SpecificationSummary specification = new Models.Specs.SpecificationSummary()
            {
                Id = specificationId,
                Name = "Specification Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

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

            calculationCodeReferenceUpdate
                .Received(calculations.Count)
                .ReplaceSourceCodeReferences(Arg.Any<Calculation>(),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name),
                    VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name));

            foreach (Calculation calculation in calculations)
            {
                calculationCodeReferenceUpdate
                    .Received(1)
                    .ReplaceSourceCodeReferences(calculation,
                        VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name),
                        VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name));
            }
        }
    }
}
