using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class ProviderDataTableBuilderTests : DataTableBuilderTest<ProviderDataTableBuilder>
    {
        [TestInitialize]
        public void SetUp()
        {
            DataTableBuilder = new ProviderDataTableBuilder();       
        }
        
        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomString().Substring(0, 32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithProvider(NewProvider(prov => 
                    prov.WithUKPRN(NewRandomStringWithMaxLength(32))
                        .WithURN(NewRandomStringWithMaxLength(32))
                        .WithLACode(NewRandomStringWithMaxLength(32))
                        .WithSuccessor(NewRandomStringWithMaxLength(32))
                        .WithTrustCode(NewRandomStringWithMaxLength(32))
                        .WithPaymentOrganisationIdentifier(NewRandomStringWithMaxLength(32)))));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithIsIndicative(false)
                .WithProvider(NewProvider(prov => 
                    prov.WithUKPRN(NewRandomStringWithMaxLength(32))
                        .WithURN(NewRandomStringWithMaxLength(32))
                        .WithLACode(NewRandomStringWithMaxLength(32))
                        .WithSuccessor(NewRandomStringWithMaxLength(32))
                        .WithTrustCode(NewRandomStringWithMaxLength(32))
                        .WithPaymentOrganisationIdentifier(NewRandomStringWithMaxLength(32)))));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("ProviderName", 256),
                NewDataColumn<string>("UKPRN", 32),
                NewDataColumn<string>("URN", 32),
                NewDataColumn<string>("Authority", 256),
                NewDataColumn<string>("ProviderType", 128),
                NewDataColumn<string>("ProviderSubType", 128),
                NewDataColumn<DateTime>("DateOpened", allowNull: true),
                NewDataColumn<DateTime>("DateClosed", allowNull: true),
                NewDataColumn<string>("LaCode", 32),
                NewDataColumn<string>("Status", 64, true),
                NewDataColumn<string>("Successor", 32, true),
                NewDataColumn<string>("TrustCode", 32, allowNull: true),
                NewDataColumn<string>("TrustName", 128, allowNull: true),
                NewDataColumn<string>("PaymentOrganisationIdentifier", 32, true),
                NewDataColumn<string>("PaymentOrganisationName", 256, true),
                NewDataColumn<bool>("IsIndicative"));
            
            Provider rowOneProvider = rowOne.Provider;
            Provider rowTwoProvider = rowTwo.Provider;
            
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, rowOne.ProviderId, rowOneProvider.Name,
                    rowOneProvider.UKPRN, rowOneProvider.URN, rowOneProvider.Authority, rowOneProvider.ProviderType, rowOneProvider.ProviderSubType, DbNullSafe(rowOneProvider.DateOpened?.UtcDateTime),
                    DbNullSafe(rowOneProvider.DateClosed?.UtcDateTime), rowOneProvider.LACode, DbNullSafe(rowOneProvider.Status), DbNullSafe(rowOneProvider.Successor), DbNullSafe(rowOneProvider.TrustCode),
                    DbNullSafe(rowOneProvider.TrustName), DbNullSafe(rowOneProvider.PaymentOrganisationIdentifier), DbNullSafe(rowOneProvider.PaymentOrganisationName), rowOne.IsIndicative),
                NewRow(rowTwo.PublishedProviderId, rowTwo.ProviderId, rowTwoProvider.Name,
                    rowTwoProvider.UKPRN, rowTwoProvider.URN, rowTwoProvider.Authority, rowTwoProvider.ProviderType, rowTwoProvider.ProviderSubType, DbNullSafe(rowTwoProvider.DateOpened?.UtcDateTime),
                    DbNullSafe(rowTwoProvider.DateClosed?.UtcDateTime), rowTwoProvider.LACode, DbNullSafe(rowTwoProvider.Status), DbNullSafe(rowTwoProvider.Successor), DbNullSafe(rowTwoProvider.TrustCode),
                    DbNullSafe(rowTwoProvider.TrustName), DbNullSafe(rowTwoProvider.PaymentOrganisationIdentifier), DbNullSafe(rowTwoProvider.PaymentOrganisationName), rowTwo.IsIndicative));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Providers]");
        }
    }
}