using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Analysis.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.Threading;
using NSubstitute;
using Polly;


namespace CalculateFunding.Services.Calcs.UnitTests.Analysis.ObsoleteItems
{
    [TestClass]
    public class ObsoleteItemServiceTests
    {
        private Mock<IUniqueIdentifierProvider> _uniqueIdentifiers;

        [TestInitialize]
        public void SetUp()
        {
            _uniqueIdentifiers = new Mock<IUniqueIdentifierProvider>();
            _uniqueIdentifiers.Setup(_ => _.CreateUniqueIdentifier())
                .Returns(() => Guid.NewGuid().ToString());
        }
        
        [TestMethod]
        public async Task CreateObsoleteItem_WhenModelIsInValid_ReturnsBadRequest()
        {
            // Arrange
            ObsoleteItem obsoleteItem = CreateModel();
            Mock<IValidator<ObsoleteItem>> validator = CreateValidator();
            IObsoleteItemService service = CreateService(obsoleteItemValidator: validator.Object);
            validator.Setup(x => x.ValidateAsync(It.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id), default(CancellationToken)))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(nameof(ObsoleteItem.SpecificationId), NewRandomString()) }));

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
            Mock<IValidator<ObsoleteItem>> validator = CreateValidator();
            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object, validator.Object);
            validator.Setup(x => x.ValidateAsync(It.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id), default(CancellationToken)))
                .ReturnsAsync(new ValidationResult());
            repository.Setup(x => x.CreateObsoleteItem(It.Is<ObsoleteItem>(x => x.Id == obsoleteItem.Id)))
                .ReturnsAsync(HttpStatusCode.Created);

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
            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync((ObsoleteItem)null);

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
            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync(CreateModel());
            repository.Setup(x => x.GetCalculationById(calculationId))
                .ReturnsAsync((Models.Calcs.Calculation)null);

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

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync(obsoleteItem);
            repository.Setup(x => x.GetCalculationById(calculationId))
                .ReturnsAsync(new Models.Calcs.Calculation() { Id = calculationId });

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<OkResult>();

            repository
                .Verify(x => x.UpdateObsoleteItem(It.Is<ObsoleteItem>(a => a.Id == obsoleteItemId)), Times.Never);
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_WhenCalculationNotExistsInObsoleteItem_ShouldAddAndUpdateObsoleteItem()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync(obsoleteItem);
            repository.Setup(x => x.GetCalculationById(calculationId))
                .ReturnsAsync(new Models.Calcs.Calculation() { Id = calculationId });
            repository.Setup(x => x.UpdateObsoleteItem(It.Is<ObsoleteItem>(x => x.Id == obsoleteItemId)))
                .ReturnsAsync(HttpStatusCode.OK);

            // Act
            IActionResult result = await service.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<OkResult>();

            repository
                .Verify(x => x.UpdateObsoleteItem(It.Is<ObsoleteItem>(a => a.Id == obsoleteItemId && a.CalculationIds.Any(x => x == calculationId)))
                , Times.Once);
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

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemsForCalculation(calculationId))
                .ReturnsAsync(new[] { obsoleteItem });

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

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemsForSpecification(specificationId))
                .ReturnsAsync(new[] { obsoleteItem });

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
            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync((ObsoleteItem)null);

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
            obsoleteItem.CalculationIds = new List<string> { calculationId, NewRandomString() };

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync(obsoleteItem);
            repository.Setup(x => x.UpdateObsoleteItem(It.Is<ObsoleteItem>(x => x.Id == obsoleteItemId)))
                .ReturnsAsync(HttpStatusCode.OK);

            // Act
            IActionResult result = await service.RemoveObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NoContentResult>();

            repository
                .Verify(x => x.UpdateObsoleteItem(It.Is<ObsoleteItem>(x => x.Id == obsoleteItemId)), Times.Once);
        }

        [TestMethod]
        public async Task RemoveObsoleteItem_WhenOnlyOneCalculation_ShouldRemoveObsoleteItem()
        {
            // Arrange
            string obsoleteItemId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem obsoleteItem = CreateModel();
            obsoleteItem.Id = obsoleteItemId;
            obsoleteItem.CalculationIds = new List<string> { calculationId };

            Mock<ICalculationsRepository> repository = CreateCalculationsRepository();
            IObsoleteItemService service = CreateService(repository.Object);
            repository.Setup(x => x.GetObsoleteItemById(obsoleteItemId))
                .ReturnsAsync(obsoleteItem);
            repository.Setup(x => x.DeleteObsoleteItem(obsoleteItemId))
                .ReturnsAsync(HttpStatusCode.NoContent);

            // Act
            IActionResult result = await service.RemoveObsoleteItem(obsoleteItemId, calculationId);

            // Assert
            result.
                Should()
                .BeOfType<NoContentResult>();

            repository
                .Verify(x => x.DeleteObsoleteItem(obsoleteItemId), Times.Once);
        }

        private IObsoleteItemService CreateService(
        ICalculationsRepository calculationsRepository = null,
        IValidator<ObsoleteItem> obsoleteItemValidator = null)
        {
            return new ObsoleteItemService(
                calculationsRepository ?? CreateCalculationsRepository().Object,
                CreateLogger().Object,
                obsoleteItemValidator ?? CreateValidator().Object,
                new ResiliencePolicies
                {
                    CalculationsRepository = Policy.NoOpAsync()
                },
                _uniqueIdentifiers.Object);
        }

        private static Mock<ICalculationsRepository> CreateCalculationsRepository()
        {
            return new Mock<ICalculationsRepository>();
        }

        private static Mock<ILogger> CreateLogger()
        {
            return new Mock<ILogger>();
        }

        private static Mock<IValidator<ObsoleteItem>> CreateValidator()
        {
            return new Mock<IValidator<ObsoleteItem>>();
        }

        private static ObsoleteItem CreateModel(string specificationId = null,
            string calculationId = null) =>
            new ObsoleteItem
            {
                Id = NewRandomString(),
                SpecificationId = specificationId ?? NewRandomString(),
                CalculationIds = new List<string>
                {
                    calculationId ?? NewRandomString()
                }
            };

        private static string NewRandomString() => new RandomString();
    }
}
