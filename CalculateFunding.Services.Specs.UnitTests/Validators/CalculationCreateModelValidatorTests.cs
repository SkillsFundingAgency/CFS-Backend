using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.Validators
{
    [TestClass]
    public class CalculationCreateModelValidatorTests
    {
        static string specificationId = Guid.NewGuid().ToString();
        static string allocationLineid = Guid.NewGuid().ToString();
        static string policyId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptySpecificationId_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

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
        }

        [TestMethod]
        public void Validate_GivenEmptyAllocationLineId_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.AllocationLineId = string.Empty;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenEmptyPolicyId_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.PolicyId = string.Empty;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

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
        }

        [TestMethod]
        public void Validate_GivenEmptyDescription_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Description = string.Empty;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

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
        }

        [TestMethod]
        public void Validate_GivenEmptyName_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Name = string.Empty;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

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
        }

        [TestMethod]
        public void Validate_GivenInvalidCalculationType_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = (CalculationType)5222;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

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
        }

        [TestMethod]
        public void Validate_GivenNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            repository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(repository);

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
        }

        [TestMethod]
        public void Validate_GivenValidModelForFundingCalculation_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculation_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();
            fundingStreams.Add(new FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al1",
                            Name= "Allocation Line 1",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs2",
                Name = "Funding Stream 2",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al2",
                            Name= "Allocation Line 2",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs3",
                Name = "Funding Stream 3",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id=allocationLineid,
                            Name= "Allocation Line which should be found",
                        }
                    }
            });

            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculationHasSpecificationThatDoesNotExist_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = null;

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();
            fundingStreams.Add(new FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al1",
                            Name= "Allocation Line 1",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs2",
                Name = "Funding Stream 2",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al2",
                            Name= "Allocation Line 2",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs3",
                Name = "Funding Stream 3",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id=allocationLineid,
                            Name= "Allocation Line which should be found",
                        }
                    }
            });

            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Specification not found");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculationHasNoAllocationLineSet_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;
            model.AllocationLineId = null;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion() { }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Select an allocation line to create this calculation specification");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculation_WhenNoFundingStreamsReturned_ThenValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "baselineCalculation1",
                                    Name = "Baseline Calculation 1",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = null;
            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Unable to query funding streams, result returned null");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculation_WhenFundingStreamProvidedNotFound_ThenValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "baselineCalculation1",
                                    Name = "Baseline Calculation 1",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();
            fundingStreams.Add(new FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al1",
                            Name= "Allocation Line 1",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs2",
                Name = "Funding Stream 2",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al2",
                            Name= "Allocation Line 2",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs3",
                Name = "Funding Stream 3",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al3",
                            Name= "Allocation Line which should be found",
                        }
                    }
            });

            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Unable to find Allocation Line with provided ID");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithBaselineCalculation_WhenAllocationLineProvidedAlreadyExistsWithinSpecification_ThenValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Baseline;
            model.AllocationLineId = "al1";

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "baselineCalculation1",
                                    Name = "Baseline Calculation 1",
                                    AllocationLine = new Reference("al1", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();
            fundingStreams.Add(new FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al1",
                            Name= "Allocation Line 1",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs2",
                Name = "Funding Stream 2",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al2",
                            Name= "Allocation Line 2",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs3",
                Name = "Funding Stream 3",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al3",
                            Name= "Allocation Line which should be found",
                        }
                    }
            });

            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("This specification already has an existing Baseline calculation associated with it. Please choose a different allocation line ID to create a Baseline calculation for.");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithNumberCalculation_WhenNoFundingStreamsReturned_ThenValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Number;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "baselineCalculation1",
                                    Name = "Baseline Calculation 1",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = null;
            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Unable to query funding streams, result returned null");
        }

        [TestMethod]
        public void Validate_GivenValidModelWithNumberCalculation_WhenFundingStreamProvidedNotFound_ThenValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.CalculationType = CalculationType.Number;

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository(false);
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "fundingCalc1",
                                    Name = "Funding Calculation",
                                    AllocationLine = new Reference(allocationLineid, "Allocation Name"),
                                    CalculationType = CalculationType.Funding,
                                },
                                new Calculation()
                                {
                                    Id = "baselineCalculation1",
                                    Name = "Baseline Calculation 1",
                                    AllocationLine = new Reference("existingBaselineAllocationLineId", "Baseline"),
                                    CalculationType = CalculationType.Baseline,
                                },
                            }
                        }
                    }
                }
            };

            specsRepo
                .GetSpecificationById(specificationId)
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();
            fundingStreams.Add(new FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al1",
                            Name= "Allocation Line 1",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs2",
                Name = "Funding Stream 2",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al2",
                            Name= "Allocation Line 2",
                        }
                    }
            });

            fundingStreams.Add(new FundingStream()
            {
                Id = "fs3",
                Name = "Funding Stream 3",
                AllocationLines = new List<AllocationLine>()
                    {
                        new AllocationLine()
                        {
                            Id="al3",
                            Name= "Allocation Line which should be found",
                        }
                    }
            });

            specsRepo
                .GetFundingStreams()
                .Returns(fundingStreams);

            CalculationCreateModelValidator validator = CreateValidator(specsRepo);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .First()
                .Should()
                .BeOfType<ValidationFailure>()
                .Which
                .ErrorMessage
                .Should()
                .Be("Unable to find Allocation Line with provided ID");
        }

        static CalculationCreateModel CreateModel()
        {
            return new CalculationCreateModel
            {
                SpecificationId = specificationId,
                AllocationLineId = allocationLineid,
                PolicyId = policyId,
                Description = description,
                Name = name,
                CalculationType = CalculationType.Funding,
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

        static CalculationCreateModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new CalculationCreateModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
