using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void GetMappedCalculationsOfSpecificationTemplate_FundingStreamIdIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string fundingStreamId = null;

            CalculationService service = CreateCalculationService(logger: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.GetMappedCalculationsOfSpecificationTemplate(specificationId, fundingStreamId);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void GetMappedCalculationsOfSpecificationTemplate_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            CalculationService service = CreateCalculationService(logger: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.GetMappedCalculationsOfSpecificationTemplate(specificationId, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetMappedCalculationsOfSpecificationTemplate_TemplateMappingNotFound_Returns404()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string fundingStreamId = "test";

            string expectedErrorMessage = $"A template mapping was not found for specification id {specificationId} and funding stream Id {fundingStreamId}";

            TemplateMapping templateMapping = null;

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetTemplateMapping(specificationId, fundingStreamId)
                .Returns(templateMapping);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository);


            // Act
            IActionResult result = await service.GetMappedCalculationsOfSpecificationTemplate(specificationId, fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be(expectedErrorMessage);
        }

        [TestMethod]
        public async Task GetMappedCalculationsOfSpecificationTemplate_TemplateMappingExistsInCache_TemplateMappingItemsReturned()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string fundingStreamId = "test";

            List<TemplateMappingItem> templateMappingItems = new List<TemplateMappingItem>();

            TemplateMapping templateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = templateMappingItems
            };

            string cacheKey = $"{CacheKeys.TemplateMapping}{specificationId}-{fundingStreamId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TemplateMapping>(cacheKey)
                .Returns(templateMapping);

            CalculationService service = CreateCalculationService(
                logger: logger,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetMappedCalculationsOfSpecificationTemplate(specificationId, fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(templateMappingItems);
        }

        [TestMethod]
        public async Task GetMappedCalculationsOfSpecificationTemplate_TemplateMappingExistsInCosmos_TemplateMappingItemsReturned()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string fundingStreamId = "test";

            List<TemplateMappingItem> templateMappingItems = new List<TemplateMappingItem>();

            TemplateMapping templateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = templateMappingItems
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetTemplateMapping(specificationId, fundingStreamId)
                .Returns(templateMapping);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository);


            // Act
            IActionResult result = await service.GetMappedCalculationsOfSpecificationTemplate(specificationId, fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(templateMappingItems);
        }
    }
}
