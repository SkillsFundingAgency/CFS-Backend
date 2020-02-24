using System;
using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class PaymentFundingLineProfileTotalsTests : ProfilingTestBase
    {
        [TestMethod]
        [DynamicData(nameof(ProfileTotalExamples), DynamicDataSourceType.Method)]
        public void SumsProfileValuesAcrossPaymentFundingLines(PublishedProviderVersion publishedProviderVersion,
            IEnumerable<ProfileTotal> expectedProfileTotals)
        {
            IEnumerable<ProfileTotal> actualProfileTotals = new PaymentFundingLineProfileTotals(publishedProviderVersion);

            actualProfileTotals
                .Should()
                .BeEquivalentTo(expectedProfileTotals);
        }

        private static IEnumerable<object[]> ProfileTotalExamples()
        {
            yield return new object[]
            {
                NewPublishedProviderVersion(_ => _.WithFundingLines(NewFundingLine(fl =>
                    fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                            dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(123)
                                    .WithTypeValue("January")
                                    .WithOccurence(0)
                                    .WithYear(2021)),
                                NewProfilePeriod(pp => pp.WithAmount(999)
                                    .WithTypeValue("January")
                                    .WithOccurence(1)
                                    .WithYear(2021)),
                                NewProfilePeriod(pp => pp.WithAmount(666)
                                    .WithTypeValue("February")
                                    .WithOccurence(0)
                                    .WithYear(2021)))))))),
                new ProfileTotal[0]
            };
            yield return new object[]
            {
                NewPublishedProviderVersion(_ => _.WithFundingLines(NewFundingLine(fl =>
                    fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(123)
                                        .WithTypeValue("January")
                                        .WithOccurence(0)
                                        .WithYear(2021)),
                                    NewProfilePeriod(pp => pp.WithAmount(999)
                                        .WithTypeValue("January")
                                        .WithOccurence(1)
                                        .WithYear(2021)),
                                    NewProfilePeriod(pp => pp.WithAmount(666)
                                        .WithTypeValue("February")
                                        .WithOccurence(0)
                                        .WithYear(2021)))))))),
                new[]
                {
                    NewProfileTotal(_ => _.WithYear(2021)
                        .WithValue(123)
                        .WithTypeValue("January")
                        .WithOccurrence(0)),
                    NewProfileTotal(_ => _.WithYear(2021)
                        .WithValue(999)
                        .WithTypeValue("January")
                        .WithOccurrence(1)),
                    NewProfileTotal(_ => _.WithYear(2021)
                        .WithValue(666)
                        .WithTypeValue("February")
                        .WithOccurrence(0))
                }
            };
            yield return new object[]
            {
                NewPublishedProviderVersion(_ => _.WithFundingLines(NewFundingLine(fl =>
                    fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Information)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                            dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(123)
                                    .WithTypeValue("January")
                                    .WithOccurence(0)
                                    .WithYear(2021)),
                                NewProfilePeriod(pp => pp.WithAmount(999)
                                    .WithTypeValue("January")
                                    .WithOccurence(1)
                                    .WithYear(2021)),
                                NewProfilePeriod(pp => pp.WithAmount(666)
                                    .WithTypeValue("February")
                                    .WithOccurence(0)
                                    .WithYear(2021)))))),
                    NewFundingLine(fl =>
                        fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(777)
                                        .WithTypeValue("January")
                                        .WithOccurence(0)
                                        .WithYear(2021)),
                                    NewProfilePeriod(pp => pp.WithAmount(566)
                                        .WithTypeValue("February")
                                        .WithOccurence(0)
                                        .WithYear(2021)))))),
                        NewFundingLine(fl =>
                            fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                    dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(1001)
                                        .WithTypeValue("January")
                                        .WithOccurence(0)
                                        .WithYear(2021)))))))),
                new[]
                {
                    NewProfileTotal(_ => _.WithYear(2021)
                        .WithValue(1778)
                        .WithTypeValue("January")
                        .WithOccurrence(0)),
                    NewProfileTotal(_ => _.WithYear(2021)
                        .WithValue(566)
                        .WithTypeValue("February")
                        .WithOccurrence(0))
                }
            };
        }

        private static ProfileTotal NewProfileTotal(Action<ProfileTotalBuilder> setUp = null)
        {
            ProfileTotalBuilder profileTotalBuilder = new ProfileTotalBuilder();

            setUp?.Invoke(profileTotalBuilder);

            return profileTotalBuilder.Build();
        }
    }
}