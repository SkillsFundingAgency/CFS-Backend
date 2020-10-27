using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class CreateDatasetDefinitionFromTemplateModelValidator : AbstractValidator<CreateDatasetDefinitionFromTemplateModel>
    {
        private readonly IPolicyRepository _policyRepository;

        public CreateDatasetDefinitionFromTemplateModelValidator(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;


            RuleFor(model => model.FundingStreamId)
                .CustomAsync(async (name, context, ct) =>
                {
                    CreateDatasetDefinitionFromTemplateModel model = context.ParentContext.InstanceToValidate as CreateDatasetDefinitionFromTemplateModel;
                    if (string.IsNullOrWhiteSpace(model.FundingStreamId))
                    {
                        context.AddFailure("Funding Stream Id must be provided");
                    }
                    else
                    {
                        IEnumerable<PoliciesApiModels.FundingStream> fundingStreams = await _policyRepository.GetFundingStreams();

                        if (fundingStreams != null && !fundingStreams.Any(_ => _.Id == model.FundingStreamId))
                        {
                            context.AddFailure($"Unable to find given funding stream ID: {model.FundingStreamId}");
                        }
                    }
                });

            RuleFor(model => model.FundingPeriodId)
               .CustomAsync(async (name, context, ct) =>
               {
                   CreateDatasetDefinitionFromTemplateModel model = context.ParentContext.InstanceToValidate as CreateDatasetDefinitionFromTemplateModel;
                   if (string.IsNullOrWhiteSpace(model.FundingPeriodId))
                   {
                       context.AddFailure("Funding Period Id must be provided");
                   }
                   else
                   {
                       PoliciesApiModels.FundingPeriod fundingPeriod = await _policyRepository.GetFundingPeriod(model.FundingPeriodId);

                       if (fundingPeriod == null)
                       {
                           context.AddFailure($"Unable to find given funding period id: {model.FundingPeriodId}");
                       }
                   }
               });
        }
    }
}
