using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Analysis.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis.ObsoleteItems
{
    [TestClass]
    public class ObsoleteItemCleanupTests
    {
        private Mock<IObsoleteReferenceCleanUp> _stepOne;
        private Mock<IObsoleteReferenceCleanUp> _stepTwo;
        private Mock<IObsoleteReferenceCleanUp> _stepThree;
        private Mock<IObsoleteReferenceCleanUp> _stepFour;
        
        private ObsoleteItemCleanup _cleanup;

        [TestInitialize]
        public void SetUp()
        {
            _stepOne = NewStep();
            _stepTwo = NewStep();
            _stepThree = NewStep();
            _stepFour = NewStep();

            _cleanup = new ObsoleteItemCleanup(new[]
            {
                _stepOne.Object,
                _stepTwo.Object,
                _stepThree.Object,
                _stepFour.Object
            });
        }
        
        [TestMethod]
        public void GuardsAgainstNoCalculationSupplied()
        {
            Action invocation = () => WhenTheCleanUpIsRunForCalculation(null)
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
        public async Task ExecutesEachConfiguredCleanUpStep()
        {
            Calculation calculation = NewCalculation();

            await WhenTheCleanUpIsRunForCalculation(calculation);
            
            ThenTheStepsProcessedTheCalculation(calculation, _stepOne, _stepTwo, _stepThree, _stepFour);
        }

        private void ThenTheStepsProcessedTheCalculation(Calculation calculation,
            params Mock<IObsoleteReferenceCleanUp>[] steps)
        {
            foreach (Mock<IObsoleteReferenceCleanUp> step in steps)
            {
                step.Verify(_ => _.ProcessCalculation(calculation),
                    Times.Once);
            }    
        }

        private async Task WhenTheCleanUpIsRunForCalculation(Calculation calculation)
            => await _cleanup.ProcessCalculation(calculation);

        private Mock<IObsoleteReferenceCleanUp> NewStep() => new Mock<IObsoleteReferenceCleanUp>();

        private Calculation NewCalculation() => new CalculationBuilder().Build();
    }
}