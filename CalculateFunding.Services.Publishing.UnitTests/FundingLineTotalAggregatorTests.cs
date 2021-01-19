using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using Serilog;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingLineTotalAggregatorTests
    {
        [TestMethod]
        public void GenerateTotals_GivenValidTemplateMetadataContentsAndCalculations_ReturnsFundingLines()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleFundingLineTemplate1.json"));

            Mock<IFundingLineRoundingSettings> rounding = new Mock<IFundingLineRoundingSettings>();

            rounding.Setup(_ => _.DecimalPlaces)
                .Returns(2);
            
            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator(rounding.Object);

            TemplateMapping mapping = CreateTemplateMappings();

            //Act
            GeneratorModels.FundingValue fundingValue = fundingLineTotalAggregator.GenerateTotals(contents, mapping.TemplateMappingItems.ToDictionary(_ => _.TemplateId), CreateCalculations(mapping).ToDictionary(_ => _.Id));

            IEnumerable<GeneratorModels.FundingLine> fundingLines = fundingValue.FundingLines.Flatten(_ => _.FundingLines);

            //Assert
            fundingLines.Single(_ => _.TemplateLineId == 1).Value
                .Should()
                .Be(16200.64M);

            fundingLines.Single(_ => _.TemplateLineId == 2).Value
                .Should()
                .Be(8000M);

            fundingLines.Single(_ => _.TemplateLineId == 3).Value
                .Should()
                .Be(3200M);

            fundingLines.Single(_ => _.TemplateLineId == 4).Value
                .Should()
                .Be(5000.64M);

            fundingLines.Single(_ => _.TemplateLineId == 5).Value
                .Should()
                .Be(null);

            fundingLines.Single(_ => _.TemplateLineId == 6).Value
                .Should()
                .Be(8000M);

            fundingLines.Single(_ => _.TemplateLineId == 7).Value
                .Should()
                .Be(500M);

            fundingLines.Single(_ => _.TemplateLineId == 8).Value
                .Should()
                .Be(1200M);
        }

        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger = null)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public TemplateMapping CreateTemplateMappings()
        {
            TemplateMapping mapping = new TemplateMapping();

            mapping.TemplateMappingItems = new List<TemplateMappingItem>
            {
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 1 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 2 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 3 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 4 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 5 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 6 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 7 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 8 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 9 },
                new TemplateMappingItem { CalculationId = Guid.NewGuid().ToString(), TemplateId = 10 }
            };

            return mapping;
        }

        public IEnumerable<CalculationResult> CreateCalculations(TemplateMapping mapping)
        {
            return mapping.TemplateMappingItems.Select(_ => new CalculationResult { Id = _.CalculationId, Value = GetValue(_.TemplateId) });
        }

        public decimal GetValue(uint templateId)
        {
            switch (templateId)
            {
                case 1:
                    {
                        return 1.7M;
                    }
                case 2:
                    {
                        return 500.001M;
                    }
                case 3:
                    {
                        return 1200M;
                    }
                case 5:
                    {
                        return 5000.639M;
                    }
                case 6:
                    {
                        return 80M;
                    }
                case 7:
                    {
                        return 20M;
                    }
                case 8:
                    {
                        return 8000M;
                    }
                case 9:
                    {
                        return 300M;
                    }
                case 10:
                    {
                        return 1500M;
                    }
            }

            return 0M;
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(FundingLineTotalAggregatorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
