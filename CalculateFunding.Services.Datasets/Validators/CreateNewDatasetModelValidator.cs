using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class CreateNewDatasetModelValidator : AbstractValidator<CreateNewDatasetModel>
    {
        private readonly IDatasetRepository _datasetsRepository;
        private readonly IPolicyRepository _policyRepository;

        private readonly IEnumerable<string> validExtensions = new[] { ".xls", ".xlsx" };

        public CreateNewDatasetModelValidator(
            IDatasetRepository datasetsRepository,
            IPolicyRepository policyRepository)
        {
            _datasetsRepository = datasetsRepository;
            _policyRepository = policyRepository;

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
                    context.AddFailure("Use a descriptive unique name other users can understand");
                else
                {
                    IEnumerable<Dataset> datasets = _datasetsRepository.GetDatasetsByQuery(m => m.Content.Name.ToLower() == model.Name.ToLower()).Result;
                    if (datasets != null && datasets.Any())
                        context.AddFailure("Use a descriptive unique name other users can understand");
                }
            });

            RuleFor(model => model.FundingStreamId)
            .Custom((name, context) =>
            {
                CreateNewDatasetModel model = context.ParentContext.InstanceToValidate as CreateNewDatasetModel;
                if (string.IsNullOrWhiteSpace(model.FundingStreamId))
                {
                    context.AddFailure("You must give a Funding Stream Id for the dataset");
                }
                else
                {
                    IEnumerable<PoliciesApiModels.FundingStream> fundingStreams = _policyRepository.GetFundingStreams().Result;

                    if (fundingStreams != null && !fundingStreams.Any(_ => _.Id == model.FundingStreamId))
                    {
                        context.AddFailure($"Unable to find given funding stream ID: {model.FundingStreamId}");
                    }
                }
            });
        }
    }
}
