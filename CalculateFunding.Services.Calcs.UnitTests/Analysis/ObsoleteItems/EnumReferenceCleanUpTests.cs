using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Analysis.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis.ObsoleteItems
{
    [TestClass]
    public class EnumReferenceCleanUpTests
    {
        private Mock<ICalculationsRepository> _calculations;

        private EnumReferenceCleanUp _cleanUp;
        
        [TestInitialize]
        public void SetUp()
        {
            _calculations = new Mock<ICalculationsRepository>();
            
            _cleanUp = new EnumReferenceCleanUp(_calculations.Object,
                new ResiliencePolicies
                {
                    CalculationsRepository = Policy.NoOpAsync(),
                    CalculationsRepositoryNoOCCRetry = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstNoCalculationBeingSupplied()
        {
            Action invocation = () => WhenTheCalculationIsProcessed(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("calculation");
        }

        [TestMethod]
        public async Task RemovesCalculationIdFromObsoleteEnumItemsWhereIsNoLongerReferenced()
        {
            string codeReferenceOne = NewRandomString();
            string codeReferenceTwo = NewRandomString();
            string calculationId = NewRandomString();
            string otherCalculationOneId = NewRandomString();
            string otherCalculationTwoId = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithCurrentVersion(NewCalculationVersion(cv =>
                    cv.WithSourceCode($"{NewRandomString()}_{codeReferenceOne}_{NewRandomString()}_{NewRandomString()}"))));

            DocumentEntity<ObsoleteItem> itemOne = NewObsoleteItemDocument(_ => _.WithCalculationIds(calculationId, otherCalculationOneId)
                .WithCodeReference(codeReferenceOne));
            DocumentEntity<ObsoleteItem> itemTwo = NewObsoleteItemDocument(_ => _.WithCalculationIds(calculationId)
                .WithCodeReference(codeReferenceTwo));
            DocumentEntity<ObsoleteItem> itemThree = NewObsoleteItemDocument(_ => _.WithCalculationIds(otherCalculationTwoId));
            
            GivenTheObsoleteItems(calculationId, ObsoleteItemType.EnumValue, itemOne, itemTwo, itemThree);

            await WhenTheCalculationIsProcessed(calculation);

            itemOne
                .Content
                .CalculationIds
                .Should()
                .BeEquivalentTo<string>(new[]
                {
                    calculationId,
                    otherCalculationOneId
                });

            itemTwo
                .Content
                .IsEmpty
                .Should()
                .BeTrue();

            itemThree
                .Content
                .CalculationIds
                .Should()
                .BeEquivalentTo(otherCalculationTwoId);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemOne.Content, It.IsAny<string>()),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.DeleteObsoleteItem(itemTwo.Id,itemTwo.ETag),
                    Times.Once);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemTwo.Content,It.IsAny<string>()),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.DeleteObsoleteItem(itemThree.Id, It.IsAny<string>()),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemThree.Content,It.IsAny<string>()), 
                    Times.Never);
        }

        private async Task WhenTheCalculationIsProcessed(Calculation calculation)
            => await _cleanUp.ProcessCalculation(calculation);
        
        private void GivenTheObsoleteItems(string calculationId,
            ObsoleteItemType obsoleteItemType,
            params DocumentEntity<ObsoleteItem>[] obsoleteItems)
        {
            _calculations.Setup(_ => _.GetObsoleteItemDocumentsForCalculation(calculationId, obsoleteItemType))
                .Returns(obsoleteItems);
        }

        private CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }

        private Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }

        private DocumentEntity<ObsoleteItem> NewObsoleteItemDocument(Action<ObsoleteItemBuilder> setUp = null)
        {
            ObsoleteItemBuilder obsoleteItemBuilder = new ObsoleteItemBuilder();

            setUp?.Invoke(obsoleteItemBuilder);

            ObsoleteItem item = obsoleteItemBuilder.Build();

            DocumentEntity<ObsoleteItem> document = new DocumentEntity<ObsoleteItem>
            {
                Content = item,
                ETag = NewRandomString()
            };
            
            return document;
        }

        private string NewRandomString() => new RandomString();
    }
}