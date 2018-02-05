using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class CreateNewDatasetModelValidator : AbstractValidator<CreateNewDatasetModel>
    {
        private readonly IDataSetsRepository _datasetsRepository;

        private IEnumerable<string> validExtensions = new[] { ".csv", ".xls", ".xlsx" };

        public CreateNewDatasetModelValidator(IDataSetsRepository datasetsRepository)
        {
            _datasetsRepository = datasetsRepository;

            RuleFor(model => model.DefinitionId)
            .NotEmpty()
            .WithMessage("You must provide a valid dataset schema");

            RuleFor(model => model.Description)
             .NotEmpty()
             .WithMessage("You must give a description for the dataset");

            RuleFor(model => model.Filename)
             .Custom((name, context) =>
             {
                 CreateNewDatasetModel model = context.ParentContext.InstanceToValidate as CreateNewDatasetModel;
                 if(string.IsNullOrWhiteSpace(model.Filename))
                     context.AddFailure("You must provide a filename");
                 else if (!validExtensions.Contains(Path.GetExtension(model.Filename.ToLower())))
                     context.AddFailure("Check you have the right file format");
             });

            RuleFor(model => model.Name)
            .Custom((name, context) =>
            {
                CreateNewDatasetModel model = context.ParentContext.InstanceToValidate as CreateNewDatasetModel;
                if (string.IsNullOrWhiteSpace(model.Name))
                    context.AddFailure("You must give a unique dataset name");
                IEnumerable<Dataset> datasets = _datasetsRepository.GetDatasetsByQuery(m => m.Name.ToLower() == model.Name.ToLower()).Result;
                if(datasets.Any())
                    context.AddFailure("You must give a unique dataset name");
            });

        }
    }
}
