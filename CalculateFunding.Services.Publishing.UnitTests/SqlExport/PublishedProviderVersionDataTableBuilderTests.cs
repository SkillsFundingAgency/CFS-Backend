using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class PublishedProviderVersionDataTableBuilderTests : DataTableBuilderTest<PublishedProviderVersionDataTableBuilder>
    {
        [TestInitialize]
        public void SetUp()
        {
            DataTableBuilder = new PublishedProviderVersionDataTableBuilder();       
        }
        
        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomString().Substring(0, 32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithTotalFunding(NewRandomUnsignedNumber())
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                .WithProvider(NewProvider(prov => 
                    prov.WithStatus(NewRandomStringWithMaxLength(32)))));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithTotalFunding(NewRandomUnsignedNumber())
                .WithAuthor(NewAuthor(auth => auth.WithName(NewRandomStringWithMaxLength(32))))
                .WithProvider(NewProvider(prov => 
                    prov.WithStatus(NewRandomStringWithMaxLength(32)))));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching( NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<decimal>("TotalFunding"),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("FundingStreamId", 32),
                NewDataColumn<string>("FundingPeriodId", 32),
                NewDataColumn<string>("MajorVersion", 32),
                NewDataColumn<string>("MinorVersion", 32),
                NewDataColumn<string>("Status", 32),
                NewDataColumn<DateTime>("LastUpdated"),
                NewDataColumn<string>("LastUpdatedBy", 256));
            
            AndTheDataTableHasRowsMatching(NewRow(rowOne.PublishedProviderId, rowOne.TotalFunding, rowOne.ProviderId, rowOne.FundingStreamId,
                    rowOne.FundingPeriodId, rowOne.MajorVersion, rowOne.MinorVersion, rowOne.Status, rowOne.Date.Date, rowOne.Author.Name),
                NewRow(rowTwo.PublishedProviderId, rowTwo.TotalFunding, rowTwo.ProviderId, rowTwo.FundingStreamId,
                    rowTwo.FundingPeriodId, rowTwo.MajorVersion, rowTwo.MinorVersion, rowTwo.Status, rowTwo.Date.Date, rowTwo.Author.Name));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Funding]");
        }

        private Reference NewAuthor(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }
    }
}