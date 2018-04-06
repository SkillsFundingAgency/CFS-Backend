using CalculateFunding.Models.Datasets;
using FluentValidation;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetMetadataModelValidator : AbstractValidator<DatasetMetadataModel>
    {
        public DatasetMetadataModelValidator()
        {
            RuleFor(model => model.DataDefinitionId)
               .NotEmpty()
               .WithMessage("Missing data definition id.");

            RuleFor(model => model.AuthorName)
              .NotEmpty()
              .WithMessage("Missing author name.");

            RuleFor(model => model.AuthorId)
              .NotEmpty()
              .WithMessage("Missing author id.");

            RuleFor(model => model.DatasetId)
              .NotEmpty()
              .WithMessage("Missing dataset id.");

            RuleFor(model => model.Name)
              .NotEmpty()
              .WithMessage("Missing dataset name.");

            RuleFor(model => model.Description)
              .NotEmpty()
              .WithMessage("Missing dataset description.");
        }
    }
}
