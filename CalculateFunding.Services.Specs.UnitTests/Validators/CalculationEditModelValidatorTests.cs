﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.MappingProfiles;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class CalculationEditModelValidatorTests
    {
        private const string calculationId = "calculationId";
        private const string specificationId = "specId";
        private const string allocationLineid = "allocationLineId";
        private const string description = "A test description";
        private const string name = "A test name";

        private static IEnumerable<object[]> ValidateBasicTestCases()
        {
            yield return new object[] { CreateModel(), true, new string[0] };
            yield return new object[] { CreateModel(_calculationId: string.Empty), false, new[] { "Calculation ID not specified on the model" } };
            yield return new object[] { CreateModel(_specificationId: string.Empty), false, new[] { "Null or empty specification Id provided" } };
            yield return new object[] { CreateModel(_allocationLineId: string.Empty), true, new string[0] };
            yield return new object[] { CreateModel(_description: string.Empty), false, new[] { "You must give a description for the calculation" } };
            yield return new object[] { CreateModel(_name: string.Empty), false, new[] { "You must give a unique calculation name" } };
            yield return new object[] { CreateModel(_calculationType: (CalculationType)5222), false, new[] { "You must specify a valid calculation type" } };
            yield return new object[] { CreateModel(_calculationType: CalculationType.Baseline, _allocationLineId: string.Empty), false, new[] { "Select an allocation line to create this calculation specification" } };
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(ValidateBasicTestCases), DynamicDataSourceType.Method)]
        public void ValidateBasic_ValidatesAsExpected(CalculationEditModel calculationEditModel, bool expectedResult, IEnumerable<string> expectedErrors)
        {
            //Arrange
            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification
            {
                Current = new SpecificationVersion()
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationEditModelValidator validator = CreateValidator(specsRepository: specsRepo);

            // Act
            ValidationResult result = validator.Validate(calculationEditModel);

            // Assert
            result
                .IsValid
                .Should()
                .Be(expectedResult);

            result
                .Errors
                .Count
                .Should()
                .Be(expectedErrors.Count());

            foreach (string error in expectedErrors)
            {
                result
                    .Errors
                    .Count(e => e.ErrorMessage == error)
                    .Should()
                    .Be(1, $"Error message collection should have included '{error}'");
            }
        }

        [TestMethod]
        public void Validate_GivenNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
            };

            repository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            ICalculationsRepository calculationsRepository = Substitute.For<ICalculationsRepository>();
            calculationsRepository
                .IsCalculationNameValid(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(false);

            CalculationEditModelValidator validator = CreateValidator(specsRepository: repository, calculationsRepository: calculationsRepository);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);

            result
                .Errors
                .Single()
                .ErrorMessage
                .Should()
                .Be("Calculation with the same generated source code name already exists in this specification");

            calculationsRepository
                .Received(1)
                .IsCalculationNameValid(specificationId, name, calculationId);
        }

        private static IEnumerable<object[]> ValidateWithSpecificationAndFundingStreamTestCases()
        {
            Specification defaultSpecification = new Specification
            {
                Current = new SpecificationVersion
                {
                     Calculations = new List<Calculation>
                            {
                                new Calculation
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                }
            };

            IEnumerable<PolicyModels.FundingStream> defaultFundingStreams = new List<PolicyModels.FundingStream>
            {
                new PolicyModels.FundingStream
                {
                    Id = "fs1",
                    Name = "Funding Stream 1",
                    AllocationLines = new List<PolicyModels.AllocationLine>
                    {
                        new PolicyModels.AllocationLine
                        {
                            Id = "al1",
                            Name = "Allocation Line 1",
                        }
                    }
                },
                new PolicyModels.FundingStream
                {
                    Id = "fs2",
                    Name = "Funding Stream 2",
                    AllocationLines = new List<PolicyModels.AllocationLine>
                    {
                        new PolicyModels.AllocationLine
                        {
                            Id = "al2",
                            Name = "Allocation Line 2",
                        }
                    }
                },
                new PolicyModels.FundingStream
                {
                    Id = "fs3",
                    Name = "Funding Stream 3",
                    AllocationLines = new List<PolicyModels.AllocationLine>
                    {
                        new PolicyModels.AllocationLine
                        {
                            Id = allocationLineid,
                            Name = "Allocation Line which should be found",
                        }
                    }
                }
            };

            foreach (var calculationType in new[] { CalculationType.Baseline, CalculationType.Number })
            {
                yield return new object[]
                {
                    CreateModel(_calculationType: calculationType),
                    defaultSpecification,
                    defaultFundingStreams,
                    true,
                    new string[0]
                };
                yield return new object[]
                {
                    CreateModel(_calculationType: calculationType, _allocationLineId: "al2"),
                    defaultSpecification,
                    defaultFundingStreams,
                    true,
                    new string[0]
                };
                yield return new object[]
                {
                    CreateModel(_calculationType: calculationType, _allocationLineId: "al2"),
                    defaultSpecification,
                    null,
                    false,
                    new[] {"Unable to query funding streams, result returned null"}
                };
                yield return new object[]
                {
                    CreateModel(_calculationType: calculationType),
                    defaultSpecification,
                    null,
                    false,
                    new[] {"Unable to query funding streams, result returned null"}
                };
                yield return new object[]
                {
                    CreateModel(_calculationType: calculationType, _allocationLineId: "Cromulent"),
                    defaultSpecification,
                    defaultFundingStreams,
                    false,
                    new[] {"Unable to find Allocation Line with provided ID"}
                };
            }
            yield return new object[]
            {
                    CreateModel(_calculationType: CalculationType.Baseline, _allocationLineId: "al1"),
                    new Specification
                    {
                        Current = new SpecificationVersion
                        {
                             Calculations = new List<Calculation>
                                    {
                                        new Calculation
                                        {
                                            Id = "fundingCalc1",
                                            Name = "Funding Calculation",
                                            AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                            CalculationType = CalculationType.Funding,
                                        },
                                        new Calculation
                                        {
                                            Id = "baselineCalculation1",
                                            Name = "Baseline Calculation 1",
                                            AllocationLine = new Reference("al1", "Baseline"),
                                            CalculationType = CalculationType.Baseline,
                                        },
                                    }
                        }
                    },
                    defaultFundingStreams,
                    false,
                    new[]
                    {
                        "This specification already has an existing Baseline calculation associated with it. Please choose a different allocation line ID to create a Baseline calculation for."
                    }
            };
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DynamicData(nameof(ValidateWithSpecificationAndFundingStreamTestCases), DynamicDataSourceType.Method)]
        public void ValidateWithSpecificationAndFundingStream_ValidatesAsExpected(CalculationEditModel calculationEditModel,
            Specification specification,
            IEnumerable<PolicyModels.FundingStream> fundingStreams,
            bool expectedResult,
            IEnumerable<string> expectedErrors)
        {
            //Arrange
            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            IPoliciesApiClient policiesApiCLient = CreatePoliciesApiClient();
            ApiResponse<IEnumerable<PolicyModels.FundingStream>> fundingStreamsResponse = new ApiResponse<IEnumerable<PolicyModels.FundingStream>>(HttpStatusCode.OK, fundingStreams);

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            policiesApiCLient
                .GetFundingStreams()
                .Returns(fundingStreamsResponse);

            CalculationEditModelValidator validator = CreateValidator(specsRepository: specsRepo, policiesApiClient: policiesApiCLient);

            // Act
            ValidationResult result = validator.Validate(calculationEditModel);

            // Assert
            result
                .IsValid
                .Should()
                .Be(expectedResult);

            result
                .Errors
                .Count
                .Should()
                .Be(expectedErrors.Count());

            foreach (string error in expectedErrors)
            {
                result
                    .Errors
                    .Count(e => e.ErrorMessage == error)
                    .Should()
                    .Be(1, $"Error message collection should have included '{error}'");
            }
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculationHasSpecificationThatDoesNotExist_ValidIsFalse()
        {
            //Arrange
            CalculationEditModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = null;

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            policiesApiClient
                .GetFundingStreams()
                .Returns(new ApiResponse<IEnumerable<PolicyModels.FundingStream>>(HttpStatusCode.OK, new List<PolicyModels.FundingStream> { new PolicyModels.FundingStream() }));

            CalculationEditModelValidator validator = CreateValidator(specsRepository: specsRepo, policiesApiClient: policiesApiClient);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            IEnumerable<string> expectedErrors = new[] { "Specification not found", "Unable to find Allocation Line with provided ID" };
            result.Errors.Count().Should().Be(expectedErrors.Count());
            foreach (string error in expectedErrors)
            {
                result.Errors.Count(e => e.ErrorMessage == error).Should().Be(1);
            }
        }

        static CalculationEditModel CreateModel(string _calculationId = null,
            string _specificationId = null,
            string _allocationLineId = null,
            string _description = null,
            string _name = null,
            CalculationType _calculationType = CalculationType.Funding)
        {
            return new CalculationEditModel
            {
                CalculationId = _calculationId ?? calculationId,
                SpecificationId = _specificationId ?? specificationId,
                AllocationLineId = _allocationLineId ?? allocationLineid,
                Description = _description ?? description,
                Name = _name ?? name,
                CalculationType = _calculationType
            };
        }

        static ISpecificationsRepository CreateSpecificationsRepository(bool hasCalculation = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(specificationId), Arg.Is(name))
                .Returns(hasCalculation ? new Calculation() : null);

            return repository;
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            IPoliciesApiClient policiesApiClient = Substitute.For<IPoliciesApiClient>();

            return policiesApiClient;
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mappingConfiguration = new MapperConfiguration(
                c =>
                {
                    c.AddProfile<PolicyMappingProfile>();
                }
            );
            IMapper mapper = mappingConfiguration.CreateMapper();
            return mapper;
        }

        private static CalculationEditModelValidator CreateValidator(
            IMapper mapper = null,
            ISpecificationsRepository specsRepository = null,
            ICalculationsRepository calculationsRepository = null,
            IPoliciesApiClient policiesApiClient = null)
        {
            return new CalculationEditModelValidator(
                mapper ?? CreateMapper(),
                specsRepository ?? CreateSpecificationsRepository(),
                calculationsRepository ?? CreateCalculationsRepository(),
                policiesApiClient ?? CreatePoliciesApiClient());
        }

        private static ICalculationsRepository CreateCalculationsRepository(bool isCalculationNameValid = true)
        {
            ICalculationsRepository calculationsRepository = Substitute.For<ICalculationsRepository>();
            calculationsRepository
                .IsCalculationNameValid(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(isCalculationNameValid);

            return calculationsRepository;
        }
    }
}
