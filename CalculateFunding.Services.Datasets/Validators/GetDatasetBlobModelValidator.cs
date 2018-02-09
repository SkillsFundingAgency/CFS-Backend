using CalculateFunding.Models.Datasets;
using FluentValidation;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class GetDatasetBlobModelValidator : AbstractValidator<GetDatasetBlobModel>
    {
        public GetDatasetBlobModelValidator()
        {
            RuleFor(model => model.DatasetId)
               .NotEmpty()
               .WithMessage("Missing data dataset id.");

            RuleFor(model => model.Filename)
              .NotEmpty()
              .WithMessage("Missing file name.");

            RuleFor(model => model.Version)
              .GreaterThan(0)
              .WithMessage("Invalid version provided.");
        }
    }
}
