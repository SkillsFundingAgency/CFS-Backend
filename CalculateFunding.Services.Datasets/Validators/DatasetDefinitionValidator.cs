using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Polly;
using System.Collections.Generic;
using System.Linq;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetDefinitionValidator : AbstractValidator<DatasetDefinition>
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly AsyncPolicy _datasetsRepositoryPolicy;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public DatasetDefinitionValidator(
            IPolicyRepository policyRepository,
            IDatasetRepository datasetRepository,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            _policyRepository = policyRepository;
            _datasetRepository = datasetRepository;
            _datasetsRepositoryPolicy = datasetsResiliencePolicies.DatasetRepository;
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();

            RuleFor(model => model.FundingStreamId)
                .CustomAsync(async (name, context, ct) =>
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

                    bool datasetWithGivenNameExists =
                        await _datasetsRepositoryPolicy.ExecuteAsync(() => _datasetRepository.DatasetExistsWithGivenName(model.Name, model.Id));

                    if (datasetWithGivenNameExists)
                    {
                        context.AddFailure($"Given dataset name already exists in repository: {model.Name}");
                    }

                    IDictionary<string, string> fieldIdentifierNameMap = new Dictionary<string, string>();

                    IEnumerable<FieldDefinition> fieldDefinitions = model.TableDefinitions?.SelectMany(_ => _.FieldDefinitions);

                    if (fieldDefinitions != null)
                    {
                        foreach (FieldDefinition fieldDefinition in fieldDefinitions)
                        {
                            string fieldDefinitionNameIdentifier = _typeIdentifierGenerator.GenerateIdentifier(fieldDefinition.Name);

                            if (fieldIdentifierNameMap.ContainsKey(fieldDefinitionNameIdentifier))
                            {
                                context.AddFailure($"Given field definition name matches another field definition name. " +
                                    $"{fieldIdentifierNameMap[fieldDefinitionNameIdentifier]} and {fieldDefinition.Name}");
                            }
                            else
                            {
                                fieldIdentifierNameMap.Add(fieldDefinitionNameIdentifier, fieldDefinition.Name);
                            }
                        }
                    }
                });
        }
    }
}
