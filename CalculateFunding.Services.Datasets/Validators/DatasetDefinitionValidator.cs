using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetDefinitionValidator : AbstractValidator<DatasetDefinition>
    {
        private readonly IPolicyRepository _policyRepository;

        public DatasetDefinitionValidator(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;


            RuleFor(model => model.FundingStreamId)
                .Custom((name, context) =>
                {
                    DatasetDefinition model = context.ParentContext.InstanceToValidate as DatasetDefinition;
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
