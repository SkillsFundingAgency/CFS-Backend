using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class FundingStreamPaymentDatesQueryTests
    {
        private Mock<IFundingStreamPaymentDatesRepository> _paymentDates;
        private FundingStreamPaymentDatesQuery _paymentDatesQuery;

        [TestInitialize]
        public void SetUp()
        {
            _paymentDates = new Mock<IFundingStreamPaymentDatesRepository>();
            
            _paymentDatesQuery = new FundingStreamPaymentDatesQuery(_paymentDates.Object,
                new ResiliencePolicies
                {
                    FundingStreamPaymentDatesRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        [DynamicData(nameof(MissingParameterExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingParameters(string fundingStreamId,
            string fundingPeriodId,
            string expectedMissingParameterName)
        {
            Func<Task<IActionResult>> invocation = () => WhenThePaymentDatesAreQueried(fundingStreamId, 
                fundingPeriodId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(expectedMissingParameterName);
        }
        
        [TestMethod]
        public async Task ReturnsNotFoundResultIfNothingFoundForSuppliedFundingStreamAndPeriod()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            IActionResult result = await WhenThePaymentDatesAreQueried(fundingStreamId, fundingPeriodId);

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task DelegatesToRepositoryAndReturnsOkObjectResult()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            FundingStreamPaymentDates expectedPaymentDates = NewPaymentDates();
            
            GivenThePaymentDates(expectedPaymentDates, fundingStreamId, fundingPeriodId);

            OkObjectResult result = await WhenThePaymentDatesAreQueried(fundingStreamId, fundingPeriodId) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeSameAs(expectedPaymentDates);
        }

        private static IEnumerable<object[]> MissingParameterExamples()
        {
            yield return new object [] {null, NewRandomString(), "fundingStreamId"};
            yield return new object [] {"", NewRandomString(), "fundingStreamId"};
            yield return new object [] {string.Empty, NewRandomString(), "fundingStreamId"};
            yield return new object [] {NewRandomString(), null, "fundingPeriodId"};
            yield return new object [] {NewRandomString(), "", "fundingPeriodId"};
            yield return new object [] {NewRandomString(), string.Empty, "fundingPeriodId"};
        }

        private void GivenThePaymentDates(FundingStreamPaymentDates fundingStreamPaymentDates,
            string fundingStreamId,
            string fundingPeriodId)
        {
            _paymentDates.Setup(_ => _.GetUpdateDates(fundingStreamId,
                    fundingPeriodId))
                .ReturnsAsync(fundingStreamPaymentDates);
        }

        private async Task<IActionResult> WhenThePaymentDatesAreQueried(string fundingStreamId,
            string fundingPeriodId)
        {
            return await _paymentDatesQuery.GetFundingStreamPaymentDates(fundingStreamId, fundingPeriodId);
        }
        
        private FundingStreamPaymentDates NewPaymentDates() => new FundingStreamPaymentDatesBuilder()
            .Build();
        
        private static string NewRandomString() => new RandomString();
    }
    
    [TestClass]
    public class FundingStreamPaymentDatesIngestionTests
    {
        private Mock<IFundingStreamPaymentDatesRepository> _paymentDates;
        private Mock<ICsvUtils> _csvUtils;

        private FundingStreamPaymentDatesIngestion _ingestion; 

        [TestInitialize]
        public void SetUp()
        {
            _csvUtils = new Mock<ICsvUtils>();
            _paymentDates = new Mock<IFundingStreamPaymentDatesRepository>();
            
            _ingestion = new FundingStreamPaymentDatesIngestion(_paymentDates.Object,
                new ResiliencePolicies
                {
                    FundingStreamPaymentDatesRepository = Policy.NoOpAsync()
                }, 
                _csvUtils.Object,
                Logger.None);
        }

        [TestMethod]
        [DynamicData(nameof(MissingStringExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoCsvStringSupplied(string csv)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCsvIsIngested(csv,
                NewRandomString(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("paymentDatesCsv");
        }
        
        [TestMethod]
        [DynamicData(nameof(MissingStringExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoFundingStreamIdSupplied(string fundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCsvIsIngested(NewRandomString(),
                fundingStreamId,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }
        
        [TestMethod]
        [DynamicData(nameof(MissingStringExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNoFundingPeriodIdSupplied(string fundingPeriodId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCsvIsIngested(NewRandomString(),
                NewRandomString(),
                fundingPeriodId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingPeriodId");
        }

        [TestMethod]
        public async Task TransformsCsvIntoPaymentDatesAndSavesToCosmos()
        {
            string csv = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            FundingStreamPaymentDate[] expectedDates = NewDates(NewFundingStreamPaymentDate(), NewFundingStreamPaymentDate());
            
            GivenTheCsvTransformsInto(csv, expectedDates);

            IActionResult result = await WhenTheCsvIsIngested(csv, fundingStreamId, fundingPeriodId);

            result
                .Should()
                .BeOfType<OkResult>();
            
            _paymentDates
                .Verify(_ => _.SaveFundingStreamUpdatedDates(It.Is<FundingStreamPaymentDates>(pd =>
                    pd.FundingPeriodId == fundingPeriodId &&
                    pd.FundingStreamId == fundingStreamId &&
                    pd.PaymentDates.SequenceEqual(expectedDates))),
                    Times.Once);
        }

        private void GivenTheCsvTransformsInto(string csv, IEnumerable<FundingStreamPaymentDate> dates)
        {
            _csvUtils.Setup(_ => _.AsPocos<FundingStreamPaymentDate>(csv, "dd/MM/yyyy"))
                .Returns(dates);
        }

        private async Task<IActionResult> WhenTheCsvIsIngested(string csv, 
            string fundingStreamId, 
            string fundingPeriodId)
        {
            return await _ingestion.IngestFundingStreamPaymentDates(csv, fundingStreamId, fundingPeriodId);
        }
        
        private static IEnumerable<object[]> MissingStringExamples()
        {
            yield return new object[] {null};
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }
        
        private FundingStreamPaymentDate[] NewDates(params FundingStreamPaymentDate[] dates) => dates;
        
        private FundingStreamPaymentDate NewFundingStreamPaymentDate() => new FundingStreamPaymentDateBuilder()
            .Build();
        
        private string NewRandomString() => new RandomString();
    }
}