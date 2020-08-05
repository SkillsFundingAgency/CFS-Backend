using System;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class DataDownloadServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;

        private DataDownloadService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            
            _service = new DataDownloadService(_publishingArea.Object);
        }

        [TestMethod]
        public void GuardsAgainstNoDatasetCodeBeingSupplied()
        {
            Func<Task<object>> invocation = () => WhenTheDataForDatasetIsQueried(null, NewRandomNumber());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("datasetCode");
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoMatchFromRepository()
        {
            NotFoundObjectResult result = await WhenTheDataForDatasetIsQueried(NewRandomString(), NewRandomNumber()) as NotFoundObjectResult;

            result
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task ReturnsMatchingDatasetFromRepository()
        {
            string code = NewRandomString();
            int version = NewRandomNumber();

            string tableName = NewRandomString();
            object dataset = new object();
            
            GivenTheTableNameForDataset(code, version, tableName);
            AndTheMatchingDataset(tableName, dataset);
            
            OkObjectResult result = await WhenTheDataForDatasetIsQueried(code, version) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeSameAs(dataset);
        }

        private void GivenTheTableNameForDataset(string datasetCode,
            int version,
            string expectedTableName)
        {
            _publishingArea.Setup(_ => _.GetTableNameForDataset(datasetCode, version))
                .ReturnsAsync(expectedTableName);

        }

        private void AndTheMatchingDataset(string tableName,
            object expectedDataset)
        {
            _publishingArea.Setup(_ => _.GetDataForTable(tableName))
                .ReturnsAsync(expectedDataset);
        }

        private async Task<object> WhenTheDataForDatasetIsQueried(string datasetCode,
            int version)
            => await _service.GetDataForDataset(datasetCode, version);

        private string NewRandomString() => new RandomString();
        
        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

    }
}