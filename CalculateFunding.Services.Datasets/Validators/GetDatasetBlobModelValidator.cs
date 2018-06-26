using CalculateFunding.Models.Datasets;
using FluentValidation;
using FluentValidation.Validators;

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

            RuleFor(model => model.Comment)
                .Custom((value, context) =>
                {
                    GetDatasetBlobModel model = context.ParentContext.InstanceToValidate as GetDatasetBlobModel;
                    if(model.Version > 1)
                    {
                        if (string.IsNullOrWhiteSpace(model.Comment))
                        {
                            context.AddFailure("You must enter a change comment");
                        }
                    }
                });

            RuleFor(model => model.Description)
                .Custom((value, context) =>
                {
                    GetDatasetBlobModel model = context.ParentContext.InstanceToValidate as GetDatasetBlobModel;
                    if (model.Version > 1)
                    {
                        if (string.IsNullOrWhiteSpace(model.Description))
                        {
                            context.AddFailure("You must enter a description");
                        }
                    }
                });
        }
    }
}
