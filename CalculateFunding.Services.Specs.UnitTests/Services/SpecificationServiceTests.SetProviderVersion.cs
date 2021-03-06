﻿using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task SetSpecificationProviderVersion_GivenValidationErrors_ReturnsBadRequest()
        {
            //Arrange
            ValidationResult validationResult = new ValidationResult(new[]{new ValidationFailure("prop1", "any error")
                });

            IValidator<AssignSpecificationProviderVersionModel> validator = CreateAssignSpecificationProviderVersionModelValidator(validationResult);
            SpecificationsService service = CreateService(assignSpecificationProviderVersionModelValidator: validator);

            //Act

            IActionResult result = await service.SetProviderVersion(new AssignSpecificationProviderVersionModel(SpecificationId, NewRandomString()), new Reference());

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SetSpecificationProviderVersion_GivenSpecificationNotFoundErrors_ReturnsNotFound()
        {
            //Arrange
            ValidationResult validationResult = new ValidationResult(new[]{new ValidationFailure("SpecificationId", $"Specification not found for SpecificationId - {SpecificationId}")
                });

            IValidator<AssignSpecificationProviderVersionModel> validator = CreateAssignSpecificationProviderVersionModelValidator(validationResult);
            SpecificationsService service = CreateService(assignSpecificationProviderVersionModelValidator: validator);

            //Act

            IActionResult result = await service.SetProviderVersion(new AssignSpecificationProviderVersionModel(SpecificationId, NewRandomString()), new Reference());

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundObjectResult>();
        }

        [TestMethod]
        public async Task SetSpecificationProviderVersion_GivenSpecificationProviderVersionUpdated_ReturnsOkResult()
        {
            //Arrange
            string providerVersionId = NewRandomString();
            ValidationResult validationResult = new ValidationResult();

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.ProviderVersionId = providerVersionId;

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Is<SpecificationVersion>(s => s.ProviderVersionId == providerVersionId), Arg.Any<SpecificationVersion>(), null, false)
                .Returns(newSpecVersion);

            specificationsRepository
               .UpdateSpecification(Arg.Is<Specification>(s => s.Current.ProviderVersionId == providerVersionId))
               .Returns(HttpStatusCode.OK);

            IValidator<AssignSpecificationProviderVersionModel> validator = CreateAssignSpecificationProviderVersionModelValidator(validationResult);
            SpecificationsService service = CreateService(assignSpecificationProviderVersionModelValidator: validator, specificationsRepository: specificationsRepository, specificationVersionRepository: versionRepository);

            //Act
            IActionResult result = await service.SetProviderVersion(new AssignSpecificationProviderVersionModel(SpecificationId, providerVersionId), new Reference());

            //Arrange
            result
                .Should()
                .BeOfType<OkResult>();
        }
    }
}