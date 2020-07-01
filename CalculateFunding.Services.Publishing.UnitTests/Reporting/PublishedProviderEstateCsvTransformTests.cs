using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate;
using CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class PublishedProviderEstateCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedProviderEstateCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedProviderEstateCsvTransform();
        }

        [TestMethod]
        public void SupportsGeneratePublishedProviderEstateCsvJob()
        {
            _transformation.IsForJobDefinition(JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob)
                .Should()
                .Be(true);
        }

        [TestMethod]
        public void GeneratesCsvRows()
        {
            DateTime openedDate1 = new RandomDateTime();
            DateTime closedDate1 = new RandomDateTime();

            IEnumerable<PublishedProviderVersion> publishedProviderVersions =
                    NewPublishedProviderVersions(ppv => ppv.WithProvider(
                            NewProvider(pr => pr
                                .WithSuccessor("successor1")
                                .WithName("prname1")
                                .WithLACode("lacode1")
                                .WithAuthority("laname1")
                                .WithURN("urn1")
                                .WithUKPRN("ukprn1")
                                .WithProviderType("pt1")
                                .WithProviderSubType("pst1")
                                .WithDateOpened(openedDate1)
                                .WithDateClosed(closedDate1)
                                .WithReasonEstablishmentOpened("openReason1")
                                .WithReasonEstablishmentClosed("closeReason1")
                                .WithTrustCode("trustCode1")
                                .WithTrustName("trustName1"))
                            )
                        .WithAuthor(NewReference(auth => auth.WithName("author1")))
                        .WithVariationReasons(new[] { VariationReason.LACodeFieldUpdated, VariationReason.MiddleSuperOutputAreaCodeFieldUpdated })
                        .WithVersion(2)
                        .WithProviderId("providerId")
                        .WithPublishedProviderStatus(PublishedProviderStatus.Updated),
                    ppv => ppv.WithProvider(
                            NewProvider(pr => pr
                                .WithSuccessor("")
                                .WithName("prname2")
                                .WithLACode("lacode2")
                                .WithAuthority("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithDateOpened(null)
                                .WithDateClosed(null)
                                .WithReasonEstablishmentClosed("closeReason2")
                                .WithProviderType("pt2")
                                .WithProviderSubType("pst2")
                                .WithTrustCode("trustCode2")
                                .WithTrustName("trustName2")))
                        .WithAuthor(NewReference(auth => auth.WithName("author2")))
                        .WithVersion(1)
                        .WithProviderId("providerId")
                        .WithPublishedProviderStatus(PublishedProviderStatus.Released));
            dynamic[] expectedCsvRows =
            {
                new Dictionary<string, object>
                {
                    {"UKPRN", "ukprn1"},
                    {"Provider Has Successor", "True"},
                    {"Provider Data Changed", "True"},
                    {"Provider Has Closed", "True"},
                    {"Provider Has Opened", "True"},
                    {"Variation Reason", "LACodeFieldUpdated|MiddleSuperOutputAreaCodeFieldUpdated"},
                    {"Current URN", "urn1"},
                    {"Current Provider Name", "prname1"},
                    {"Current Provider Type", "pt1"},
                    {"Current Provider Subtype", "pst1"},
                    {"Current LA Code", "lacode1"},
                    {"Current LA Name", "laname1"},
                    {"Current Open Date", openedDate1.ToString("s")},
                    {"Current Open Reason", "openReason1"},
                    {"Current Close Date", closedDate1.ToString("s")},
                    {"Current Close Reason", "closeReason1"},
                    {"Current Successor Provider ID", "successor1"},
                    {"Current Trust Code", "trustCode1"},
                    {"Current Trust Name", "trustName1"},
                    {"Previous URN", "urn2"},
                    {"Previous Provider Name", "prname2"},
                    {"Previous Provider Type", "pt2"},
                    {"Previous Provider Subtype", "pst2"},
                    {"Previous LA Code", "lacode2"},
                    {"Previous LA Name", "laname2"},
                    {"Previous Close Date", null},
                    {"Previous Close Reason", "closeReason2"},
                    {"Previous Trust Code", "trustCode2"},
                    {"Previous Trust Name", "trustName2"},
                }
            };

            IEnumerable<IGrouping<string, PublishedProviderVersion>> publishedProviderVersionGroup 
                = publishedProviderVersions.GroupBy(x => x.ProviderId);

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation
                .Transform(publishedProviderVersionGroup)
                .ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }

        private static IEnumerable<PublishedProviderVersion> NewPublishedProviderVersions(
            params Action<PublishedProviderVersionBuilder>[] setUps)
        {
            return setUps.Select(NewPublishedProvider);
        }

        private static PublishedProviderVersion NewPublishedProvider(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }
    }
}