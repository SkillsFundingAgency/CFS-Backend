using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateMetadataContents = CalculateFunding.Common.TemplateMetadata.Models.TemplateMetadataContents;
using TemplateMetadataGenerator = CalculateFunding.Common.TemplateMetadata.Schema11.TemplateMetadataGenerator;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class InformationLineAggregatorTests
    {
        private IInformationLineAggregator _informationLineAggregator;
        private TemplateMetadataGenerator _templateMetadataGenerator;

        [TestInitialize]
        public void SetUp()
        {
            Mock<ILogger> logger = new Mock<ILogger>();
            _informationLineAggregator = new InformationLineAggregator();
            _templateMetadataGenerator = new TemplateMetadataGenerator(logger.Object);
        }

        [TestMethod]
        public void InformationLineAggregator_GivenInformationFundingLineReturnsSumOfChildPaymentFundingLines()
        {
            TemplateMetadataContents templateMetadataContents = _templateMetadataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplateInformation1_Schema1_1.json"));

            string distributionPeriodId = new RandomString();

            FundingLine[] fundingLines = new FundingLine[] { 
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

            ProfilePeriod[] profiles = _informationLineAggregator.Sum(templateMetadataContents.RootFundingLines.First(), fundingLines.ToDictionary(_ => _.TemplateLineId));

            profiles
                .First()
                .ProfiledValue
                .Should()
                .Be(2500);

            profiles
                .Skip(1)
                .First()
                .ProfiledValue
                .Should()
                .Be(500);
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
