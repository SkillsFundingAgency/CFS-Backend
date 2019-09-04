using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderDataGeneratorTests
    {
        [TestMethod]
        public void GenerateTotals_GivenValidTemplateMetadataContentsCalculationsAndProviders_ReturnsFundingLines()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleFundingLineTemplate1.json"));

            IMapper mapper = CreateMapper();

            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator(mapper);

            TemplateMapping mapping = CreateTemplateMappings();

            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(fundingLineTotalAggregator, mapper);

            //Act
            Dictionary<string, GeneratedProviderResult> generatedProviderResult = publishedProviderDataGenerator.Generate(contents, mapping, GetProviders(), CreateCalculations(mapping));

            //Assert
            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 1).Value
                .Should()
                .Be(16500.63M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 1).Value
                .Should()
                .Be(1.7M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 2).Value
                .Should()
                .Be(8000M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 2).Value
                .Should()
                .Be(500M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 3).Value
                .Should()
                .Be(3500M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 3).Value
                .Should()
                .Be(1200M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 4).Value
                .Should()
                .Be(5000.63M);

            generatedProviderResult["1234"].FundingLines.Single(_ => _.TemplateLineId == 5).Value
                .Should()
                .Be(0M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 5).Value
                .Should()
                .Be(5000.63M);

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
                .Be(1500M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 8).Value
                .Should()
                .Be(8000M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 9).Value
                .Should()
                .Be(300M);

            generatedProviderResult["1234"].Calculations.Single(_ => _.TemplateCalculationId == 10).Value
                .Should()
                .Be(1500M);

            generatedProviderResult["1234"].ReferenceData.Single(_ => _.TemplateReferenceId == 1).Value
                .Should()
                .Be("1");
        }

        [TestMethod]
        public void GenerateTotals_GivenValidTemplateMetadataContentsAndProvidersButMissingCalculations_EmptyGeneratedProviderResultsReturned()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleFundingLineTemplate1.json"));

            IMapper mapper = CreateMapper();

            FundingLineTotalAggregator fundingLineTotalAggregator = new FundingLineTotalAggregator(mapper);

            TemplateMapping mapping = CreateTemplateMappings();

            PublishedProviderDataGenerator publishedProviderDataGenerator = new PublishedProviderDataGenerator(fundingLineTotalAggregator, mapper);

            //Act
            Dictionary<string, GeneratedProviderResult> generatedProviderResult = publishedProviderDataGenerator.Generate(contents, mapping, GetProviders(), new ProviderCalculationResult[0]);

            generatedProviderResult.Any()
                .Should()
                .BeFalse();
        }


        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger = null)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }

        public IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<GeneratorsMappingProfile>();
            });

            return new Mapper(config);
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IEnumerable<Common.ApiClient.Providers.Models.Provider> GetProviders()
        {
            return new List<Common.ApiClient.Providers.Models.Provider> { new Common.ApiClient.Providers.Models.Provider { ProviderId = "1234", Name = "Provider 1" } };
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

        public IEnumerable<ProviderCalculationResult> CreateCalculations(TemplateMapping mapping)
        {
            return new List<ProviderCalculationResult> { new ProviderCalculationResult { ProviderId = "1234", Results = mapping.TemplateMappingItems.Select(_ => new CalculationResult { Id = _.CalculationId, Value = GetValue(_.TemplateId) }) } };
        }

        public decimal GetValue(uint templateId)
        {
            switch(templateId)
            {
                case 1:
                    {
                        return 1.7M;
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
                        return 5000.63M;
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
