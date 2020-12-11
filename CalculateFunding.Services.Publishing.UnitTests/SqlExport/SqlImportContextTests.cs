using System;
using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImportContextTests
    {
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _providers;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _funding;
        private Mock<IDictionary<uint, IDataTableBuilder<PublishedProviderVersion>>> _profiling;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _paymentFundingLines;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _informationFundingLines;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _calculations;

        private SqlImportContext _importContext;

        [TestInitialize]
        public void SetUp()
        {
            _funding = new Mock<IDataTableBuilder<PublishedProviderVersion>>();
            _providers = new Mock<IDataTableBuilder<PublishedProviderVersion>>();
            _informationFundingLines = new Mock<IDataTableBuilder<PublishedProviderVersion>>();
            _paymentFundingLines = new Mock<IDataTableBuilder<PublishedProviderVersion>>();
            _calculations = new Mock<IDataTableBuilder<PublishedProviderVersion>>();

            _importContext = new SqlImportContext
            {
                Funding = _funding.Object,
                Providers = _providers.Object,
                InformationFundingLines = _informationFundingLines.Object,
                PaymentFundingLines = _paymentFundingLines.Object,
                Calculations = _calculations.Object
            };
        }

        [TestMethod]
        public void AddsSuppliedPublishedProviderVersionsToEachDataTableBuilder()
        {
            PublishedProviderVersion one = NewPublishedProviderVersion();
            PublishedProviderVersion two = NewPublishedProviderVersion();
            PublishedProviderVersion three = NewPublishedProviderVersion();

            WhenTheRowsAreAdded(one, two, three);

            ThenTheRowsWereAddedToEachDataTableBuilder(one, two, three);
        }

        [TestMethod]
        public void EnsuresThereIsADataTableBuilderForEachPaymentFundingLine()
        {
            FundingLine fundingLineOne = NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment));
            FundingLine fundingLineTwo = NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment));

            PublishedProviderVersion one = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Information)),
                    fundingLineOne,
                    fundingLineTwo));

            WhenTheRowsAreAdded(one);
            
            ThenThereIsAProfilingDataTableForEachPaymentFundingLine(fundingLineOne, fundingLineTwo);
        }

        private void ThenThereIsAProfilingDataTableForEachPaymentFundingLine(params FundingLine[] fundingLines)
        {
            _importContext.Profiling
                .Should()
                .NotBeNull();
           
            foreach (FundingLine fundingLine in fundingLines)
            {
                _importContext.Profiling.ContainsKey(fundingLine.TemplateLineId)
                    .Should()
                    .BeTrue();

                IDataTableBuilder<PublishedProviderVersion> dataTableBuilder = _importContext.Profiling[fundingLine.TemplateLineId];
                
                dataTableBuilder
                    .Should()
                    .NotBeNull();

                dataTableBuilder
                    .DataTable?
                    .Rows
                    .Count
                    .Should()
                    .Be(1);
            }
        }

        private void ThenTheRowsWereAddedToEachDataTableBuilder(params PublishedProviderVersion[] publishedProviderVersions)
        {
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                _funding.Verify(_ => _.AddRows(publishedProviderVersion), Times.Once);
                _providers.Verify(_ => _.AddRows(publishedProviderVersion), Times.Once);
                _informationFundingLines.Verify(_ => _.AddRows(publishedProviderVersion), Times.Once);
                _paymentFundingLines.Verify(_ => _.AddRows(publishedProviderVersion), Times.Once);
                _calculations.Verify(_ => _.AddRows(publishedProviderVersion), Times.Once);
            }
        }

        private void WhenTheRowsAreAdded(params PublishedProviderVersion[] publishedProviderVersions)
        {
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                _importContext.AddRows(publishedProviderVersion);
            }
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder()
                .WithProviderId(NewRandomString());

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder
                .Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder
                .Build();
        }
        
        private static string NewRandomString() => new RandomString();
    }
}