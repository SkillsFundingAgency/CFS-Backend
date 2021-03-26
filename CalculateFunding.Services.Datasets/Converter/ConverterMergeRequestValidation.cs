using CalculateFunding.Models.Datasets.Converter;
using FluentValidation;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterMergeRequestValidation : AbstractValidator<ConverterMergeRequest>
    {
        public ConverterMergeRequestValidation()
        {
            RuleFor(_ => _.ProviderVersionId)
                .NotEmpty();
            RuleFor(_ => _.DatasetId)
                .NotEmpty();
            RuleFor(_ => _.Version)
                .NotEmpty();
            RuleFor(_ => _.DatasetRelationshipId)
                .NotEmpty();
        }
    }
}