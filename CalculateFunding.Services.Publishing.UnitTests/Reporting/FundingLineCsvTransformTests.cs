using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting
{
    [TestClass]
    public class FundingLineCsvTransformTests
    {
        private FundingLineCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new FundingLineCsvTransform();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, true)]
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
            IEnumerable<PublishedProvider> publishedProviders = NewPublishedProviders(_ => _.WithCurrent(
                    NewPublishedProviderVersion(ppv => ppv.WithProvider(
                            NewProvider(pr => pr.WithEstablishmentNumber("en1")
                                .WithName("prname1")
                                .WithLACode("lacode1")
                                .WithLocalAuthorityName("laname1")
                                .WithURN("urn1")
                                .WithUKPRN("ukprn1")
                                .WithProviderType("pt1")
                                .WithProviderSubType("pst1")))
                        .WithFundingLines(NewFundingLine(fl => fl.WithName("fl1")
                                .WithValue(999M)),
                            NewFundingLine(fl => fl.WithName("fl2")
                                .WithValue(666M)))
                        .WithAuthor(NewReference(auth => auth.WithName("author1")))
                        .WithMajorVersion(1)
                        .WithMinorVersion(11)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                        .WithDate("2020-01-05T20:05:44"))),
                _ => _.WithCurrent(
                    NewPublishedProviderVersion(ppv => ppv.WithProvider(
                            NewProvider(pr => pr.WithEstablishmentNumber("en2")
                                .WithLACode("lacode2")
                                .WithName("prname2")
                                .WithLocalAuthorityName("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithProviderType("pt2")
                                .WithProviderSubType("pst2")))
                        .WithFundingLines(NewFundingLine(fl => fl.WithName("fl1")
                            .WithValue(123M)))
                        .WithAuthor(NewReference(auth => auth.WithName("author2")))
                        .WithMajorVersion(2)
                        .WithMinorVersion(21)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Approved)
                        .WithDate("2020-02-05T20:03:55"))));

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
                    {"fl1", 999M.ToString(CultureInfo.InvariantCulture)},
                    {"fl2", 666M.ToString(CultureInfo.InvariantCulture)}
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
                    {"fl1", 123M.ToString(CultureInfo.InvariantCulture)},
                }
            };

            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(publishedProviders).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }

        private static IEnumerable<PublishedProvider> NewPublishedProviders(params Action<PublishedProviderBuilder>[] setUps)
        {
            return setUps.Select(NewPublishedProvider);
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }
    }
}