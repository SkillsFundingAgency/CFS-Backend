using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedProviderFundingLineCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedProviderFundingLineCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedProviderFundingLineCsvTransform();
        }

        [TestMethod]
        [DataRow(FundingLineCsvGeneratorJobType.Undefined, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentState, true)]
        [DataRow(FundingLineCsvGeneratorJobType.HistoryProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.CurrentProfileValues, false)]
        [DataRow(FundingLineCsvGeneratorJobType.Released, true)]
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
            IEnumerable<PublishedProvider> publishedProviders = NewPublishedProviders(_ => _.WithCurrent(
                    NewPublishedProviderVersion(ppv => ppv.WithProvider(
                            NewProvider(pr => pr.WithEstablishmentNumber("en1")
                                .WithName("prname1")
                                .WithLACode("lacode1")
                                .WithAuthority("laname1")
                                .WithURN("urn1")
                                .WithUKPRN("ukprn1")
                                .WithProviderType("pt1")
                                .WithProviderSubType("pst1")
                                .WithStatus("Open")
                                .WithSuccessor("Successor1")))
                        .WithIsIndicative(true)
                        .WithFundingLines(NewFundingLine(fl => fl.WithName("bfl1")
                                .WithValue(999M)),
                            NewFundingLine(fl => fl.WithName("afl2")
                                .WithValue(666M)))
                        .WithPredecessors(new[] {"Pre1", "Pre2"})
                        .WithVariationReasons(new[] { VariationReason.AuthorityFieldUpdated, VariationReason.CensusWardCodeFieldUpdated})
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
                                .WithAuthority("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithProviderType("pt2")
                                .WithProviderSubType("pst2")
                                .WithStatus("Open")))
                        .WithFundingLines(NewFundingLine(fl => fl.WithName("zfl1")
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
                    {"Is Indicative", "True"},
                    {"afl2", 666M.ToString(CultureInfo.InvariantCulture)}, //funding lines to be alpha numerically ordered on name
                    {"bfl1", 999M.ToString(CultureInfo.InvariantCulture)},
                    {"Provider Status", "Open"},
                    {"Provider Successor", "Successor1" },
                    {"Provider Predecessors", "Pre1;Pre2" },
                    {"Provider Variation Reasons", "AuthorityFieldUpdated;CensusWardCodeFieldUpdated" }
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
                    {"Is Indicative", "False"},
                    {"zfl1", 123M.ToString(CultureInfo.InvariantCulture)},
                    {"Provider Status", "Open"},
                    {"Provider Successor", string.Empty },
                    {"Provider Predecessors", string.Empty },
                    {"Provider Variation Reasons", string.Empty }
                }
            };


            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(publishedProviders, FundingLineCsvGeneratorJobType.CurrentState).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }
    }
}