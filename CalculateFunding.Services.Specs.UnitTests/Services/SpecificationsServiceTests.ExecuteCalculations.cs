using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CalculateFunding.Services.Specs.Services
{
	public partial class SpecificationsServiceTests
	{
		const string CalculationProgressPrependKey = "calculation-progress:";

		[TestMethod]
		public async Task ExecuteCalculations_GivenRequestParametersAreEmpty_ShouldReturnBadRequestObjectResult()
		{
			// Arrange
			SpecificationsService specificationsService = CreateService();
			HttpRequest httpRequest = Substitute.For<HttpRequest>();

			httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
			{
				{ "specificationIds", new StringValues("") }
			}));

			// Act
			IActionResult actionResultReturned = await specificationsService.ExecuteCalculations(httpRequest);

			// Assert
			actionResultReturned.Should().BeOfType<BadRequestObjectResult>();
		}

		[TestMethod]
		public async Task ExecuteCalculations_GivenValidRequestParameters_ShouldReturnNoContentResult()
		{
			// Arrange
			const string specificationId1 = "123";
			const string specificationId2 = "333";

			HttpRequest httpRequest = Substitute.For<HttpRequest>();

			ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
			mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

			SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 = new SpecificationCalculationExecutionStatus(specificationId1, 0, CalculationProgressStatus.NotStarted);
			SpecificationCalculationExecutionStatus expectedSpecificationStatusCall2 = new SpecificationCalculationExecutionStatus(specificationId2, 0, CalculationProgressStatus.NotStarted);

			ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

			SpecificationsService specificationsService = CreateService(specificationsRepository: mockSpecificationsRepository,
				cacheProvider: mockCacheProvider);

			httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
			{
				{ "specificationIds", new StringValues($"{specificationId1},{specificationId2}") }
			}));

			// Act
			IActionResult actionResultReturned = await specificationsService.ExecuteCalculations(httpRequest);

			// Assert
			actionResultReturned.Should().BeOfType<NoContentResult>();
			mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId1}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
			mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId2}", expectedSpecificationStatusCall2, TimeSpan.FromHours(6), false);
			
		}

		[TestMethod]
		public async Task ExecuteCalculations_WhenACallToCalculateResultThrowsAnException_ShouldReturnInternalServerError()
		{
			// Arrange
			IMessengerService messengerService = Substitute.For<IMessengerService>();
			messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

			ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
			mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

			SpecificationsService specificationsService = CreateService(messengerService: messengerService,specificationsRepository: mockSpecificationsRepository);
			HttpRequest httpRequest = Substitute.For<HttpRequest>();
			httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
			{
				{ "specificationIds", new StringValues("123, 333, 422, 122") }
			}));

			// Act
			IActionResult actionResultReturned = await specificationsService.ExecuteCalculations(httpRequest);

			// Assert
			actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
		}

		[TestMethod]
		public async Task ExecuteCalculations_GivenASpecificationIdThatDoesNotExist_ShouldReturnBadRequestObjectResult()
		{
			// Arrange
			const string validSpecificationId = "123";
			const string invalidSpecificationId = "333";

			IMessengerService messengerService = Substitute.For<IMessengerService>();

			ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
			mockSpecificationsRepository.GetSpecificationById(validSpecificationId).Returns(new Specification());

			SpecificationsService specificationsService = CreateService(messengerService: messengerService);
			HttpRequest httpRequest = Substitute.For<HttpRequest>();
			httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
			{
				{ "specificationIds", new StringValues($"{validSpecificationId}, {invalidSpecificationId}") }
			}));

			// Act
			IActionResult actionResultReturned = await specificationsService.ExecuteCalculations(httpRequest);

			// Assert
			actionResultReturned.Should().BeOfType<BadRequestObjectResult>();
		}

		[TestMethod]
		public async Task ExecuteCalculations_WhenACallToCalculateResultThrowsAnException_ShouldReportOnCacheWithError()
		{
			// Arrange
			const string specificationId = "123";

			IMessengerService messengerService = Substitute.For<IMessengerService>();
			messengerService.SendToQueue(Arg.Any<string>(), Arg.Any<string>(), new Dictionary<string, string>()).ThrowsForAnyArgs(new ArgumentException());

			SpecificationCalculationExecutionStatus expectedSpecificationStatusCall1 =
				new SpecificationCalculationExecutionStatus(specificationId, 0, CalculationProgressStatus.Error)
				{
					ErrorMessage = $"Failed to queue publishing of provider results for specification id: {specificationId}"
				};

			ICacheProvider mockCacheProvider = Substitute.For<ICacheProvider>();

			ISpecificationsRepository mockSpecificationsRepository = Substitute.For<ISpecificationsRepository>();
			mockSpecificationsRepository.GetSpecificationById(Arg.Any<string>()).Returns(new Specification());

			SpecificationsService specificationsService = CreateService(messengerService: messengerService, specificationsRepository: mockSpecificationsRepository, cacheProvider: mockCacheProvider);
			HttpRequest httpRequest = Substitute.For<HttpRequest>();
			httpRequest.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>()
			{
				{ "specificationIds", new StringValues(specificationId) }
			}));

			// Act
			IActionResult actionResultReturned = await specificationsService.ExecuteCalculations(httpRequest);

			// Assert
			actionResultReturned.Should().BeOfType<InternalServerErrorResult>();
			mockCacheProvider.Received().SetAsync($"{CalculationProgressPrependKey}{specificationId}", expectedSpecificationStatusCall1, TimeSpan.FromHours(6), false);
		}
	}
}
