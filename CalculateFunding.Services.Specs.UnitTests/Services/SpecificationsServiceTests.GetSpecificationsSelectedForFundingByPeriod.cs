using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq;
using Newtonsoft.Json;
using CalculateFunding.Models;
using System.Linq.Expressions;
using System;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetSpecificationsSelectedForFundingByPeriod_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger(); 

            SpecificationsService service = CreateService(logs: logger);

            // Act
            IActionResult result = await service.GetSpecificationsSelectedForFundingByPeriod(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No funding period was provided to GetSpecificationsSelectedForFundingPeriod"));
        }


        [TestMethod]
        public async Task GetSpecificationsSelectedForFundingByPeriod_GivenSpecificationWasNotFound_ReturnsNotFound()
        {
            //Arrange
            SpecificationVersion sv1 = new SpecificationVersion { SpecificationId = "spec1", FundingPeriod = new Models.Reference { Id = "18/19", Name = "18/19" } };
            SpecificationVersion sv2 = new SpecificationVersion { SpecificationId = "spec2", FundingPeriod = new Models.Reference { Id = "17/18", Name = "17/18" } };

            Specification spec1 = new Specification { Id = "spec1", IsSelectedForFunding = true, Current = sv1 };
            Specification spec2 = new Specification { Id = "spec2", IsSelectedForFunding = true, Current = sv2 };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingPeriodId", new StringValues(FundingPeriodId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns((Enumerable.Empty<Specification>()));

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsSelectedForFundingByPeriod(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();       
            
            logger
                .Received(1)
                .Information(Arg.Is($"Specification was not found for funding period: {FundingPeriodId}"));
        }

        [TestMethod]
        public async Task GetSpecificationsSelectedForFundingByPeriod_GivenSpecificationWasFound_ReturnsSpecification()
        {
            //Arrange
    
            SpecificationVersion sv1 = new SpecificationVersion{ SpecificationId ="spec1", FundingPeriod = new Models.Reference{ Id="18/19", Name="18/19" }  };
            SpecificationVersion sv2 = new SpecificationVersion { SpecificationId = "spec2", FundingPeriod = new Models.Reference { Id = "17/18", Name = "17/18" } };

            Specification spec1 = new Specification { Id = "spec1" , IsSelectedForFunding = true, Current = sv1 };
            Specification spec2 = new Specification { Id = "spec2", IsSelectedForFunding = true, Current = sv2 };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingPeriodId", new StringValues(FundingPeriodId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression <Func<Specification, bool>>>())
                .Returns(new[] { spec1});

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsSelectedForFundingByPeriod(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<SpecificationSummary> summaries = okObjectResult.Value as IEnumerable<SpecificationSummary>;

            summaries
               .Count()
               .Should()
               .Be(1);
        }

    }
}
