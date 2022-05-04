using CalculateFunding.Common.TemplateMetadata.Schema11;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Linq;
using TemplateMetadataContents = CalculateFunding.Common.TemplateMetadata.Models.TemplateMetadataContents;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class InformationLinesAggregationServiceTests
    {
        private IInformationLineAggregator _informationLineAggregator;
        private IInformationLinesAggregationService _informationLinesAggregationService;
        private TemplateMetadataGenerator _templateMetadataGenerator;
        private Mock<ILogger> _logger;

        [TestInitialize]
        public void SetUp()
        {
            _logger = new Mock<ILogger>();
            _informationLineAggregator = new InformationLineAggregator();
            _informationLinesAggregationService = new InformationLinesAggregationService(_informationLineAggregator, _logger.Object);
            _templateMetadataGenerator = new TemplateMetadataGenerator(_logger.Object);
        }

        [TestMethod]
        public void InformationLineAggregator_GivenInformationFundingLineReturnsSumOfChildPaymentFundingLines()
        {
            string specificationId = new RandomString();

            string providerId = new RandomString();

            TemplateMetadataContents templateMetadataContents = _templateMetadataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplateInformation1_Schema1_1.json"));

            string distributionPeriodId = new RandomString();

            FundingLine[] fundingLines = new FundingLine[] {
                NewFundingLine(_ => _.WithTemplateLineId(1)),
                NewFundingLine(_ => _.WithTemplateLineId(2)
                    .WithValue(1000)
                    .WithDistributionPeriods(
                        NewDistributionPeriod(dp => dp
                            .WithDistributionPeriodId(distributionPeriodId)
                            .WithValue(1000)
                            .WithProfilePeriods(
                                NewProfilePeriod(pp => pp
                                    .WithDistributionPeriodId(distributionPeriodId)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("April")
                                    .WithOccurence(1)
                                    .WithYear(2021)
                                    .WithAmount(500)),
                                NewProfilePeriod(pp => pp
                                    .WithDistributionPeriodId(distributionPeriodId)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("May")
                                    .WithOccurence(1)
                                    .WithYear(2021)
                                    .WithAmount(500)))))),
                NewFundingLine(_ => _.WithTemplateLineId(4)
                    .WithValue(2000)
                    .WithDistributionPeriods(
                        NewDistributionPeriod(dp => dp
                            .WithDistributionPeriodId(distributionPeriodId)
                            .WithProfilePeriods(
                                NewProfilePeriod(pp => pp
                                    .WithDistributionPeriodId(distributionPeriodId)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("April")
                                    .WithOccurence(1)
                                    .WithYear(2021)
                                    .WithAmount(2000)))))),
                NewFundingLine(_ => _.WithTemplateLineId(6)
                    .WithValue(2000)
                    .WithDistributionPeriods(
                        NewDistributionPeriod(dp => dp
                            .WithDistributionPeriodId(distributionPeriodId)
                            .WithProfilePeriods(
                                NewProfilePeriod(pp => pp
                                    .WithDistributionPeriodId(distributionPeriodId)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("April")
                                    .WithOccurence(1)
                                    .WithYear(2021)
                                    .WithAmount(2000)))))),
                NewFundingLine(_ => _.WithTemplateLineId(8)
                    .WithValue(2000)
                    .WithDistributionPeriods(
                        NewDistributionPeriod(dp => dp
                            .WithDistributionPeriodId(distributionPeriodId)
                            .WithProfilePeriods(
                                NewProfilePeriod(pp => pp
                                    .WithDistributionPeriodId(distributionPeriodId)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("April")
                                    .WithOccurence(1)
                                    .WithYear(2021)
                                    .WithAmount(2000))))))
            };

            _informationLinesAggregationService.AggregateFundingLines(specificationId, providerId, fundingLines, templateMetadataContents.RootFundingLines);

            FundingLine rootFundingLine = fundingLines
                .First(_ => _.TemplateLineId == 1);

            rootFundingLine
                .Value
                .Should()
                .Be(3000);

            rootFundingLine.DistributionPeriods
                .First()
                .Value
                .Should()
                .Be(3000);

            rootFundingLine.DistributionPeriods
                .First()
                .ProfilePeriods
                .First()
                .ProfiledValue
                .Should()
                .Be(2500);


            rootFundingLine.DistributionPeriods
                .First()
                .ProfilePeriods
                .Skip(1)
                .First()
                .ProfiledValue
                .Should()
                .Be(500);

            _logger.Verify(_ => _.Information("Aggregation calculated for specification:{specificationId}, provider:{providerId}, funding line:{templateLineId} and value:{fundingLineJson}", specificationId, providerId, rootFundingLine.TemplateLineId, rootFundingLine.AsJson(false)), Times.Once);
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setup = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setup?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        private static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setup = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setup?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(FundingLineTotalAggregatorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
