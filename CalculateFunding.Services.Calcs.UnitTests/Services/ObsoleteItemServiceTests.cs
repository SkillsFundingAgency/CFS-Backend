using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class ObsoleteItemServiceTests
    {
        [TestMethod]
        public async Task CreateObsoleteItem_WhenModelIsInValid_ReturnsBadRequest()
        {
            // Arrange
            ObsoleteItem obsoleteItem = CreateModel();
            IValidator<ObsoleteItem> validator = CreateValidator();
            IObsoleteItemService service = CreateService(obsoleteItemValidator: validator);
            validator.ValidateAsync(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id))
                .Returns(new ValidationResult(new[] { new ValidationFailure(nameof(ObsoleteItem.SpecificationId), NewRandomString()) }));

            // Act
            IActionResult result = await service.CreateObsoleteItem(obsoleteItem);

            // Assert
            result.
                Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CreateObsoleteItem_WhenModelIsValidAndStoredInRepository_ReturnsCreated()
        {
            // Arrange
            ObsoleteItem obsoleteItem = CreateModel();
            IValidator<ObsoleteItem> validator = CreateValidator();
            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository, validator);
            validator.ValidateAsync(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id))
                .Returns(new ValidationResult());
            repository.CreateObsoleteItem(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id))
                .Returns(HttpStatusCode.Created);

            // Act
            IActionResult result = await service.CreateObsoleteItem(obsoleteItem);

            // Assert
            result.
                Should()
                .BeOfType<CreatedResult>();
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_WhenObsoleteItemNotFound_RetrunsNotFound()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns((ObsoleteItem)null);

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Obsolete item not found for given obsolete item id - {obsoleteItemId}");
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_WhenCalculationNotFound_RetrunsNotFound()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns(CreateModel());
            repository.GetCalculationById(Arg.Is(calculationId))
                .Returns((Models.Calcs.Calculation)null);

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Calculation not found for given calculation id - {calculationId}");
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_WhenCalculationAlreadyExists_ShouldNotAddAndUpdate()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.CalculationIds = new List<string>() { calculationId };

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns(obsoleteItem);
            repository.GetCalculationById(Arg.Is(calculationId))
                .Returns(new Models.Calcs.Calculation() { Id = calculationId });

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<OkResult>();

            await repository
                .DidNotReceive()
                .UpdateObsoleteItem(Arg.Is<ObsoleteItem>(a => a.Id == obsoleteItemId));
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_WhenCalculationNotExistsInObsoleteItem_ShouldAddAndUpdateObsoleteItem()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns(obsoleteItem);
            repository.GetCalculationById(Arg.Is(calculationId))
                .Returns(new Models.Calcs.Calculation() { Id = calculationId });
            repository.UpdateObsoleteItem(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItemId))
                .Returns(HttpStatusCode.OK);

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<OkResult>();

            await repository
                .Received(1)
                .UpdateObsoleteItem(Arg.Is<ObsoleteItem>(a => a.Id == obsoleteItemId && a.CalculationIds.Any(x => x == calculationId)));
        }
        
        [TestMethod]
        public async Task GetObsoleteItemsForCalculation_WhenObsoleteItemExists_ReturnOkResult()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.CalculationIds = new[] { calculationId };

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemsForCalculation(Arg.Is(calculationId))
                .Returns(new[] { obsoleteItem });

            // Act
            IActionResult result = await service.GetObsoleteItemsForCalculation(calculationId);

            // Assert
            result.
                Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(new[] { obsoleteItem });
        }

        [TestMethod]
        public async Task GetObsoleteItemsForSpecification_WhenObsoleteItemExists_ReturnOkResult()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string specificationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.SpecificationId = specificationId;

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemsForSpecification(Arg.Is(specificationId))
                .Returns(new[] { obsoleteItem });

            // Act
            IActionResult result = await service.GetObsoleteItemsForSpecification(specificationId);

            // Assert
            result.
                Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(new[] { obsoleteItem });
        }

        [TestMethod]
        public async Task RemoveObsoleteItem_WhenObsoleteItemNotFound_RetrunsNotFound()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns((ObsoleteItem)null);

            // Act
            IActionResult result = await service.RemoveObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Obsolete item not found for given obsolete item id - {obsoleteItemId}");
        }

        [TestMethod]
        public async Task RemoveObsoleteItem_WhenMoreOrThanOneCalculation_ShouldRemoveAndUpdateObsoleteItem()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.CalculationIds = new[] { calculationId, NewRandomString() };

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns(obsoleteItem);
            repository.UpdateObsoleteItem(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItemId))
                .Returns(HttpStatusCode.OK);

            // Act
            IActionResult result = await service.RemoveObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NoContentResult>();

            await repository
                .Received(1)
                .UpdateObsoleteItem(Arg.Is<ObsoleteItem>(x => x.Id == obsoleteItemId));
        }

        [TestMethod]
        public async Task RemoveObsoleteItem_WhenOnlyOneCalculation_ShouldRemoveObsoleteItem()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.CalculationIds = new[] { calculationId };

            ICalculationsRepository repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository);
            repository.GetObsoleteItemById(Arg.Is(obsoleteItemId))
                .Returns(obsoleteItem);
            repository.DeleteObsoleteItem(Arg.Is(obsoleteItemId))
                .Returns(HttpStatusCode.NoContent);

            // Act
            IActionResult result = await service.RemoveObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NoContentResult>();

            await repository
                .Received(1)
                .DeleteObsoleteItem(Arg.Is(obsoleteItemId));
        }

        private static IObsoleteItemService CreateService(
        ICalculationsRepository calculationsRepository = null,
        IValidator<ObsoleteItem> obsoleteItemValidator = null)
        {
            return new ObsoleteItemService(
                calculationsRepository ?? CreateCalculationsRepository(),
                CreateLogger(),
                obsoleteItemValidator ?? CreateValidator());
        }

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IValidator<ObsoleteItem> CreateValidator()
        {
            return Substitute.For<IValidator<ObsoleteItem>>();
        }

        private static ObsoleteItem CreateModel(string specificationId = null, string calculationId = null)
        {
            return new ObsoleteItem
            {
                Id = NewRandomString(),
                SpecificationId = specificationId ?? NewRandomString(),
                CalculationIds = new[] { calculationId ?? NewRandomString() }
            };
        }

        private static string NewRandomString() => new RandomString();
    }
}
