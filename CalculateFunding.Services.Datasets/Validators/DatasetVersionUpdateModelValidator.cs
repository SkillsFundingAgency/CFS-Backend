using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetVersionUpdateModelValidator : AbstractValidator<DatasetVersionUpdateModel>
    {
        private readonly IEnumerable<string> validExtensions = new[] { ".csv", ".xls", ".xlsx" };
        private readonly IPolicyRepository _policyRepository;

        public DatasetVersionUpdateModelValidator(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;

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

            RuleFor(model => model.FundingStreamId)
            .Custom((name, context) =>
            {
                DatasetVersionUpdateModel model = context.ParentContext.InstanceToValidate as DatasetVersionUpdateModel;
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
