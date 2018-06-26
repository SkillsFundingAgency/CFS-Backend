using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetVersionUpdateModelValidator : AbstractValidator<DatasetVersionUpdateModel>
    {
        private IEnumerable<string> validExtensions = new[] { ".csv", ".xls", ".xlsx" };

        public DatasetVersionUpdateModelValidator()
        {
            RuleFor(model => model.Filename)
             .Custom((name, context) =>
             {
                 DatasetVersionUpdateModel model = context.ParentContext.InstanceToValidate as DatasetVersionUpdateModel;
                 if(string.IsNullOrWhiteSpace(model.Filename))
                     context.AddFailure("You must provide a filename");
                 else if (!validExtensions.Contains(Path.GetExtension(model.Filename.ToLower())))
                     context.AddFailure("Check you have the right file format");
             });

            RuleFor(model => model.DatasetId)
            .NotEmpty()
            .WithMessage("You must give a datasetId");
        }
    }
}
