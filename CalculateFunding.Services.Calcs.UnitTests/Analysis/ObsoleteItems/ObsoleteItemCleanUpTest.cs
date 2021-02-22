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
    public abstract class ObsoleteItemCleanUpTest
    {
        protected Mock<ICalculationsRepository> Calculations;
        
        protected IObsoleteReferenceCleanUp CleanUp;

        protected ObsoleteItemType ObsoleteItemType;

        [TestInitialize]
        public void ObsoleteItemCleanUpTestSetUp()
        {
            Calculations = new Mock<ICalculationsRepository>();
            
            CleanUp = new EnumReferenceCleanUp(Calculations.Object,
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
        public async Task RemovesCalculationIdFromObsoleteItemsWhereIsNoLongerReferenced()
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
            
            GivenTheObsoleteItems(calculationId, itemOne, itemTwo, itemThree);

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
            
            Calculations
                .Verify(_ => _.UpdateObsoleteItem(itemOne.Content, It.IsAny<string>()),
                    Times.Never);
            
            Calculations
                .Verify(_ => _.DeleteObsoleteItem(itemTwo.Id,itemTwo.ETag),
                    Times.Once);
            
            Calculations
                .Verify(_ => _.UpdateObsoleteItem(itemTwo.Content,It.IsAny<string>()),
                    Times.Never);
            
            Calculations
                .Verify(_ => _.DeleteObsoleteItem(itemThree.Id, It.IsAny<string>()),
                    Times.Never);
            
            Calculations
                .Verify(_ => _.UpdateObsoleteItem(itemThree.Content,It.IsAny<string>()), 
                    Times.Never);
        }

        protected void GivenTheObsoleteItems(string calculationId,
            params DocumentEntity<ObsoleteItem>[] obsoleteItems)
        {
            Calculations.Setup(_ => _.GetObsoleteItemDocumentsForCalculation(calculationId, ObsoleteItemType))
                .Returns(obsoleteItems);
        }

        protected DocumentEntity<ObsoleteItem> NewObsoleteItemDocument(Action<ObsoleteItemBuilder> setUp = null)
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

        protected string NewRandomString() => new RandomString();

        protected CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }

        protected Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }

        private async Task WhenTheCalculationIsProcessed(Calculation calculation)
            => await CleanUp.ProcessCalculation(calculation);
    }
}