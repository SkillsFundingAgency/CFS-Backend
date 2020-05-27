using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class GetDatasetBlobModelValidator : AbstractValidator<GetDatasetBlobModel>
    {
        private readonly IPolicyRepository _policyRepository;

        public GetDatasetBlobModelValidator(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;

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

            RuleFor(model => model.FundingStreamId)
            .Custom((name, context) =>
            {
                GetDatasetBlobModel model = context.ParentContext.InstanceToValidate as GetDatasetBlobModel;
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
