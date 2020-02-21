using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public abstract class GraphRepositoryTestBase
    {
        protected Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        protected Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);

            return specificationBuilder.Build();
        }
    }
}
