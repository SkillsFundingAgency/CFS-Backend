using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    [TestClass]
    public class PublishedGroupsFundingLineCsvTransformTests : FundingLineCsvTransformTestBase
    {
        private PublishedGroupsFundingLineCsvTransform _transformation;

        [TestInitialize]
        public void SetUp()
        {
            _transformation = new PublishedGroupsFundingLineCsvTransform();
        }

        [TestMethod]

        [DataRow(FundingLineCsvGeneratorJobType.PublishedGroups, true)]
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
            PublishedFunding publishedFunding =
                NewPublishedFunding(_ => _.WithCurrent(
                    NewPublishedFundingVersion(ppv => ppv.WithFundingId("f-id1")
                                                         .WithMajor(1)
                                                         .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Contracting)
                                                         .WithOrganisationGroupTypeCode(Generators.OrganisationGroup.Enums.OrganisationGroupTypeCode.AcademyTrust)
                                                         .WithOrganisationGroupName("g-name1")
                                                         .WithOrganisationGroupTypeIdentifier(Generators.OrganisationGroup.Enums.OrganisationGroupTypeIdentifier.AcademyTrustCode)
                                                         .WithOrganisationGroupIdentifierValue("groupValue1")
                                                         .WithOrganisationGroupTypeClassification(Generators.OrganisationGroup.Enums.OrganisationGroupTypeClassification.GeographicalBoundary)
                                                         .WithTotalFunding(100)
                                                         .WithAuthor(new Common.Models.Reference("aid", "aname"))
                                                         .WithStatusChangedDate(new System.DateTime(2020,07,10))
                                                         )));
            

            IEnumerable<PublishedProvider> publishedProviders = NewPublishedProviders(_ => _.WithReleased(
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
                        .WithFundingStreamId("fs1")
                        .WithFundingPeriodId("fp1")
                       .WithFundingLines(NewFundingLine(fl => fl.WithName("bfl1")
                               .WithValue(999M)),
                           NewFundingLine(fl => fl.WithName("afl2")
                               .WithValue(666M)))
                       .WithProviderId("pid1")
                       .WithPredecessors(new[] { "Pre1", "Pre2" })
                       .WithVariationReasons(new[] { VariationReason.AuthorityFieldUpdated, VariationReason.CensusWardCodeFieldUpdated })
                       .WithAuthor(NewReference(auth => auth.WithName("author1")))
                       .WithMajorVersion(1)
                       .WithMinorVersion(11)
                       .WithTotalFunding(1)
                       .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                       .WithDate("2020-01-05T20:05:44")
                       .WithVariationReasons(new[] {VariationReason.AuthorityFieldUpdated, VariationReason.CensusWardCodeFieldUpdated }))));

            IEnumerable<PublishedFundingWithProvider> publishedFundingsWithProviders = new List<PublishedFundingWithProvider>
            {
               new PublishedFundingWithProvider()
               {
                   PublishedFunding = publishedFunding,
                   PublishedProviders = publishedProviders
               }
            };

            dynamic[] expectedCsvRows =
            {
                new Dictionary<string, object>
                {
                    {"Funding ID", "f-id1"},
                    {"Funding Major Version", 1},
                    {"Grouping Reason", "Contracting"},
                    {"Grouping Code", "AcademyTrust"},
                    {"Grouping Name", "g-name1"},
                    {"Grouping Type Identifier", "AcademyTrustCode"},
                    {"Grouping Identifier Value", "groupValue1"},
                    {"Grouping Type Classification", "GeographicalBoundary" },
                    {"Grouping Total Funding",100m },
                    {"Author", "aname" },
                    {"Release Date",  new DateTime(2020,07,10).ToString("s")},
                    {"Provider Count", 0 },
                    {"Provider Funding ID", "fs1-fp1-pid1-1_11"},
                    {"Provider Id", "pid1"},
                    {"Provider Name", "prname1"},
                    {"Provider Major Version", 1},
                    {"Provider Total Funding", 1m},
                    {"Provider UKPRN", "ukprn1"},
                    {"Provider URN", "urn1" },
                    {"Provider UPIN", null },
                    {"Provider LACode","lacode1" },
                    {"Provider Status","Open"},
                    {"Provider Successor","Successor1"},
                    {"Provider Predecessors","Pre1;Pre2"},
                    {"Provider Variation Reasons","AuthorityFieldUpdated;CensusWardCodeFieldUpdated"}
                }
            };


            ExpandoObject[] transformProviderResultsIntoCsvRows = _transformation.Transform(publishedFundingsWithProviders, FundingLineCsvGeneratorJobType.PublishedGroups, null).ToArray();

            transformProviderResultsIntoCsvRows
                .Should()
                .BeEquivalentTo(expectedCsvRows,
                    cfg => cfg.WithStrictOrdering());
        }
    }
}
