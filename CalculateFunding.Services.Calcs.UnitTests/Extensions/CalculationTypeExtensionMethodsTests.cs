using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Models.Calcs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;

namespace CalculateFunding.Services.Calcs.UnitTests.Extensions
{
    [TestClass]
    public class CalculationTypeExtensionMethodsTests
    {
        [TestMethod]
        [DynamicData(nameof(ToCalculationDataTypeExamples), DynamicDataSourceType.Method)]
        public void MapsCalculationTypeToDatatype(CalculationType calculationType,
            CalculationDataType expectedDataType)
        {
            calculationType.ToCalculationDataType()
                .Should()
                .Be(expectedDataType);
        }

        private static IEnumerable<object[]> ToCalculationDataTypeExamples()
        {
            yield return CalculationDataTypeExample(CalculationType.Boolean, CalculationDataType.Boolean);
            yield return CalculationDataTypeExample(CalculationType.Adjustment, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Cash, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Drilldown, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Information, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.LumpSum, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Number, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.PerPupilFunding, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.ProviderLedFunding, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.PupilNumber, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Rate, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Scope, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Weighting, CalculationDataType.Decimal);
            yield return CalculationDataTypeExample(CalculationType.Enum, CalculationDataType.Enum);
        }

        private static object[] CalculationDataTypeExample(CalculationType calculationType,
            CalculationDataType calculationDataType)
            => new object[] {calculationType, calculationDataType};

    }
}