using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
		[TestMethod]
		public async Task GetCalculationByCalculationSpecificationId_GivenAnInvalidRequest_ShouldReturnBadRequest()
		{
			//Arrange
			HttpRequest request = Substitute.For<HttpRequest>();

			ILogger logger = CreateLogger();

			CalculationService service = CreateCalculationService(logger: logger);

			//Act
			IActionResult result = await service.GetCalculationByCalculationSpecificationId(null);

			//Assert
			result
				.Should()
				.BeOfType<BadRequestObjectResult>();
		}

	    [TestMethod]
	    public async Task GetCalculationByCalculationSpecificationId_GivenACalculationIsFound_ShouldReturnOkObjectResult()
	    {
			//Arrange
		    const string calcSpecId = "CalcSpecId";

		    ICalculationsRepository mockCalculationRepository = Substitute.For<ICalculationsRepository>();
		    mockCalculationRepository
			    .GetCalculationByCalculationSpecificationId(calcSpecId)
			    .Returns(Task.FromResult(new Calculation()));

			CalculationService service = CreateCalculationService(calculationsRepository: mockCalculationRepository);

		    //Act
		    IActionResult result = await service.GetCalculationByCalculationSpecificationId(calcSpecId);

		    //Assert
		    result
			    .Should()
			    .BeOfType<OkObjectResult>();
	    }

	    [TestMethod]
	    public async Task GetCalculationByCalculationSpecificationId_GivenACalculationIsNotFound_ShouldReturnNotFoundObjectResult()
	    {
		    //Arrange
		    const string calcSpecId = "CalcSpecId";

		    ICalculationsRepository mockCalculationRepository = Substitute.For<ICalculationsRepository>();
		    mockCalculationRepository
			    .GetCalculationByCalculationSpecificationId(calcSpecId)
			    .Returns(Task.FromResult((Calculation)null));

		    CalculationService service = CreateCalculationService(calculationsRepository: mockCalculationRepository);

		    //Act
		    IActionResult result = await service.GetCalculationByCalculationSpecificationId(calcSpecId);

		    //Assert
		    result
			    .Should()
			    .BeOfType<NotFoundObjectResult>();
	    }
	}
}
