using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderDataGeneratorTests
    {
        [TestMethod]
        public async Task GenerateTotals_GivenValidTemplateMetadataContentsCalculationsAndProviders_ReturnsFundingLines()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleFundingLineTemplate1.json"));

            IMapper mapper = CreateMapper();

            Mock<IFundingLineRoundingSettings> rounding = new Mock<IFundingLineRoundingSettings>();

            rounding.Setup(_ => _.DecimalPlaces)
                .Returns(2);

            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator(rounding.Object);

            TemplateMapping mapping = CreateTemplateMappings();

            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(logger, fundingLineTotalAggregator, mapper);

            //Act
            IDictionary<string, GeneratedProviderResult> generatedProviderResult = publishedProviderDataGenerator.Generate(contents, mapping, GetProviders(), CreateCalculations(mapping));

            //Assert
            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 1).Value
                .Should()
                .Be(16200.64M); //the 5000.635 figure should be midpoint rounded away from zero to 5000.64

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 1).Value
                .Should()
                .Be(1.704M); //should be no rounding as is not Cash calc (is pupil number)

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 2).Value
                .Should()
                .Be(8000M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 2).Value
                .Should()
                .Be(500M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 3).Value
                .Should()
                .Be(3200M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 3).Value
                .Should()
                .Be(1200M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 4).Value
                .Should()
                .Be(5000.64M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 5).Value
                .Should()
                .Be(null);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 5).Value
                .Should()
                .Be(5000.635M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 6).Value
                .Should()
                .Be(8000M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 6).Value
                .Should()
                .Be(80M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 7).Value
                .Should()
                .Be(500M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 7).Value
                .Should()
                .Be(20M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 8).Value
                .Should()
                .Be(1200M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 8).Value
                .Should()
                .Be(8000M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 9).Value
                .Should()
                .Be(300M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 10).Value
                .Should()
                .Be(1500M);
        }

        [TestMethod]
        public async Task GenerateTotals_GivenValidTemplateMetadataContentsAndProvidersButMissingCalculations_EmptyGeneratedProviderResultsReturned()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleFundingLineTemplate1.json"));

            IMapper mapper = CreateMapper();

            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator(new Mock<IFundingLineRoundingSettings>().Object);

            TemplateMapping mapping = CreateTemplateMappings();

            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(logger, fundingLineTotalAggregator, mapper);

            //Act
            Dictionary<string, ProviderCalculationResult> providerCalculationResults = new Dictionary<string, ProviderCalculationResult>();

            IDictionary<string, GeneratedProviderResult> generatedProviderResult = publishedProviderDataGenerator.Generate(contents, mapping, GetProviders(), providerCalculationResults);

            generatedProviderResult.Any()
                .Should()
                .BeFalse();
        }


        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }

        public IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            return new Mapper(config);
        }

        public static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IEnumerable<Provider> GetProviders()
        {
            return new List<Provider> { new Provider { ProviderId = "1234", Name = "Provider 1" } };
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

        public IDictionary<string, ProviderCalculationResult> CreateCalculations(TemplateMapping mapping)
        {
            Dictionary<string, ProviderCalculationResult> providerCalculationResult = new Dictionary<string, ProviderCalculationResult>();

            providerCalculationResult.Add("1234", new ProviderCalculationResult { ProviderId = "1234", Results = mapping.TemplateMappingItems.Select(_ => new CalculationResult { Id = _.CalculationId, Value = GetValue(_.TemplateId) }) });

            return providerCalculationResult;
        }

        public decimal GetValue(uint templateId)
        {
            switch (templateId)
            {
                case 1:
                    {
                        //to check midpoint away ignored for none cash amounts (this is PupilNumber)
                        return 1.704M;
                    }
                case 2:
                    {
                        return 500M;
                    }
                case 3:
                    {
                        return 1200M;
                    }
                case 5:
                    {
                        //to check midpoint away from zero rounding for cash
                        return 5000.635M;
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
                default:
                    return 0M;
            }
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(FundingLineTotalAggregatorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
