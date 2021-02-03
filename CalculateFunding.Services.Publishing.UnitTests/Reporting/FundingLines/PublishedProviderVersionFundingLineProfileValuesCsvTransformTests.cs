using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedProviderVersionFundingLineProfileValuesCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedProviderVersionFundingLineProfileValuesCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedProviderVersionFundingLineProfileValuesCsvTransform();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, false)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, true)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, false)]
        [DataRow(FundingLineCsvGeneratorJobType.History, false)]
        public void SupportsCurrentStateJobType(FundingLineCsvGeneratorJobType jobType,
            bool expectedSupportsFlag)
        {
            _transformation.IsForJobType(jobType)
                .Should()
                .Be(expectedSupportsFlag);
        }

        [TestMethod]
        public void FlattensTemplateCalculationsAndProviderMetaDataIntoRows()
        {
            IEnumerable<PublishedProviderVersion> publishedProviders = NewPublishedProviderVersions(
                    NewPublishedProviderVersion(ppv => ppv.WithProvider(
                            NewProvider(pr => pr.WithEstablishmentNumber("en1")
                                .WithName("prname1")
                                .WithLACode("lacode1")
                                .WithAuthority("laname1")
                                .WithURN("urn1")
                                .WithUKPRN("ukprn1")
                                .WithProviderType("pt1")
                                .WithProviderSubType("pst1")))
                        .WithFundingLines(NewFundingLine(fl => fl.WithValue(999M)
                                .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(
                                    NewProfilePeriod(pp => pp.WithAmount(123)
                                        .WithOccurence(1)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")
                                        .WithYear(2020)),
                                        NewProfilePeriod(pp => pp.WithAmount(456)
                                            .WithOccurence(2)
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithTypeValue("January")
                                            .WithYear(2020)))),
                            NewDistributionPeriod(dp => dp.WithProfilePeriods(
                                NewProfilePeriod(pp => pp.WithAmount(789)
                                    .WithOccurence(1)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("January")
                                    .WithYear(2021)),
                                NewProfilePeriod(pp => pp.WithAmount(101112)
                                    .WithOccurence(1)
                                    .WithType(ProfilePeriodType.CalendarMonth)
                                    .WithTypeValue("February")
                                    .WithYear(2021)))))))
                        .WithAuthor(NewReference(auth => auth.WithName("author1")))
                        .WithMajorVersion(1)
                        .WithMinorVersion(11)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                        .WithDate("2020-01-05T20:05:44")),
                    NewPublishedProviderVersion(ppv => ppv.WithProvider(
                            NewProvider(pr => pr.WithEstablishmentNumber("en2")
                                .WithLACode("lacode2")
                                .WithName("prname2")
                                .WithAuthority("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithProviderType("pt2")
                                .WithProviderSubType("pst2")))
                        .WithFundingLines(NewFundingLine(fl => fl.WithValue(666M)
                            .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(
                                    NewProfilePeriod(pp => pp.WithAmount(234)
                                        .WithOccurence(1)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")
                                        .WithYear(2020)),
                                    NewProfilePeriod(pp => pp.WithAmount(567)
                                        .WithOccurence(2)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")
                                        .WithYear(2020)))),
                                NewDistributionPeriod(dp => dp.WithProfilePeriods(
                                    NewProfilePeriod(pp => pp.WithAmount(8910)
                                        .WithOccurence(1)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("January")
                                        .WithYear(2021)),
                                    NewProfilePeriod(pp => pp.WithAmount(111213)
                                        .WithOccurence(1)
                                        .WithType(ProfilePeriodType.CalendarMonth)
                                        .WithTypeValue("February")
                                        .WithYear(2021)))))))
                        .WithAuthor(NewReference(auth => auth.WithName("author2")))
                        .WithMajorVersion(2)
                        .WithMinorVersion(21)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Approved)
                        .WithDate("2020-02-05T20:03:55")));
            dynamic[] expectedCsvRows =
            {
                new Dictionary<string, object>
                {
                    {"UKPRN", "ukprn1"},
                    {"URN", "urn1"},
                    {"Establishment Number", "en1"},
                    {"Provider Name", "prname1"},
                    {"Provider Type", "pt1"},
                    {"Provider SubType", "pst1"},
                    {"LA Code", "lacode1"},
                    {"LA Name", "laname1"},
                    {"Allocation Status", "Updated"},
                    {"Allocation Major Version", "1"},
                    {"Allocation Minor Version", "11"},
                    {"Allocation Author", "author1"},
                    {"Allocation DateTime", "2020-01-05T20:05:44"},
                    {"Total Funding", 999M}, 
                    {"2020 January 1", 123M}, 
                    {"2020 January 2", 456M}, 
                    {"2021 January 1", 789M}, 
                    {"2021 February 1", 101112M} 
                },
                new Dictionary<string, object>
                {
                    {"UKPRN", "ukprn2"},
                    {"URN", "urn2"},
                    {"Establishment Number", "en2"},
                    {"Provider Name", "prname2"},
                    {"Provider Type", "pt2"},
                    {"Provider SubType", "pst2"},
                    {"LA Code", "lacode2"}, 
                    {"LA Name", "laname2"},
                    {"Allocation Status", "Approved"},
                    {"Allocation Major Version", "2"},
                    {"Allocation Minor Version", "21"},
                    {"Allocation Author", "author2"},
                    {"Allocation DateTime", "2020-02-05T20:03:55"},
                    {"Total Funding", 666M},
                    {"2020 January 1", 234M}, 
                    {"2020 January 2", 567M}, 
                    {"2021 January 1", 8910M}, 
                    {"2021 February 1", 111213M}
                }
            };

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(publishedProviders).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }

        private IEnumerable<PublishedProviderVersion> NewPublishedProviderVersions(
            params PublishedProviderVersion[] versions) => versions;

        private DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }

        private ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);
            
            return profilePeriodBuilder.Build();
        }
    }
}