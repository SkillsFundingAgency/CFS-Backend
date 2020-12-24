using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs
{
    public static class CalculationVersionExtensionMethods
    {
        public static CalculationVersionResponseModel ToResponseModel(this CalculationVersion version)
        {
            return new CalculationVersionResponseModel()
            {
                Author = version.Author,
                CalculationId = version.Id,
                CalculationType = version.CalculationType,
                LastUpdated = version.Date,
                Name = version.Name,
                Namespace = version.Namespace,
                PublishStatus = version.PublishStatus,
                SourceCode = version.SourceCode,
                SourceCodeName = version.SourceCodeName,
                Version = version.Version,
                WasTemplateCalculation = version.WasTemplateCalculation,
                Description = version.Description,
            };
        }
    }
}
