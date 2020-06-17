using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CodeGeneration;

namespace CalculateFunding.Services.Calcs
{
    public static class CalculationExtensionMethods
    {
        public static CalculationResponseModel ToResponseModel(this Calculation calculation)
        {
            return new CalculationResponseModel
            {
                SpecificationId = calculation.SpecificationId,
                Author = calculation.Current?.Author,
                LastUpdated = calculation.Current?.Date,
                FundingStreamId = calculation.FundingStreamId,
                PublishStatus = calculation.Current.PublishStatus,
                Id = calculation.Id,
                Name = calculation.Name,
                SourceCode = calculation.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode,
                Version = calculation.Current.Version,
                CalculationType = calculation.Current.CalculationType,
                Namespace = calculation.Current.Namespace,
                ValueType = calculation.Current.ValueType,
                WasTemplateCalculation = calculation.Current.WasTemplateCalculation,
                SourceCodeName = calculation.Current.SourceCodeName,
                Description = calculation.Current.Description
            };
        }

        public static CalculationSummaryModel ToSummaryModel(this Calculation calculation)
        {
            return new CalculationSummaryModel
            {
                Id = calculation.Id,
                Name = calculation.Name,
                CalculationType = calculation.Current.CalculationType,
                Status = calculation.Current.PublishStatus,
                Version = calculation.Current.Version,
                CalculationValueType = calculation.Current.ValueType
            };
        }
    }
}
