using System;
using System.Threading.Tasks;
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
                    CalculationsRepository = Policy.NoOpAsync()
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

            ObsoleteItem itemOne = NewObsoleteItem(_ => _.WithCalculationIds(calculationId, otherCalculationOneId)
                .WithCodeReference(codeReferenceOne));
            ObsoleteItem itemTwo = NewObsoleteItem(_ => _.WithCalculationIds(calculationId)
                .WithCodeReference(codeReferenceTwo));
            ObsoleteItem itemThree = NewObsoleteItem(_ => _.WithCalculationIds(otherCalculationTwoId));
            
            GivenTheObsoleteItems(calculationId, ObsoleteItemType.EnumValue, itemOne, itemTwo, itemThree);

            await WhenTheCalculationIsProcessed(calculation);

            itemOne
                .CalculationIds
                .Should()
                .BeEquivalentTo<string>(new[]
                {
                    calculationId,
                    otherCalculationOneId
                });

            itemTwo
                .IsEmpty
                .Should()
                .BeTrue();

            itemThree
                .CalculationIds
                .Should()
                .BeEquivalentTo(otherCalculationTwoId);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemOne),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.DeleteObsoleteItem(itemTwo.Id),
                    Times.Once);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemTwo),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.DeleteObsoleteItem(itemThree.Id),
                    Times.Never);
            
            _calculations
                .Verify(_ => _.UpdateObsoleteItem(itemThree), 
                    Times.Never);
        }

        private async Task WhenTheCalculationIsProcessed(Calculation calculation)
            => await _cleanUp.ProcessCalculation(calculation);
        
        private void GivenTheObsoleteItems(string calculationId,
            ObsoleteItemType obsoleteItemType,
            params ObsoleteItem[] obsoleteItems)
        {
            _calculations.Setup(_ => _.GetObsoleteItemsForCalculation(calculationId, obsoleteItemType))
                .ReturnsAsync(obsoleteItems);
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

        private ObsoleteItem NewObsoleteItem(Action<ObsoleteItemBuilder> setUp = null)
        {
            ObsoleteItemBuilder obsoleteItemBuilder = new ObsoleteItemBuilder();

            setUp?.Invoke(obsoleteItemBuilder);
            
            return obsoleteItemBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}