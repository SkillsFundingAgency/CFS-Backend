using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class DefinitionSpecificationRelationshipTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _definitionId;
        private string _definitionName;
        private string _datasetId;
        private string _datasetName;
        private string _specificationId;
        private string _specificationName;
        private string _description;
        private bool? _converterEnabled;

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDefinitionName(string definitionName)
        {
            _definitionName = definitionName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDatasetId(string datasetId)
        {
            _datasetId = datasetId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDatasetName(string datasetName)
        {
            _datasetName = datasetName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithSpecificationName(string specificationName)
        {
            _specificationName = specificationName;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParametersBuilder WithConverterEnabled(bool converterEnabled)
        {
            _converterEnabled = converterEnabled;

            return this;
        }

        public DefinitionSpecificationRelationshipTemplateParameters Build()
            => new DefinitionSpecificationRelationshipTemplateParameters
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                DefinitionId = _definitionId ?? NewRandomString(),
                DefinitionName = _definitionName ?? NewRandomString(),
                DatasetId = _datasetId ?? NewRandomString(),
                DatasetName = _datasetName ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                SpecificationName = _specificationName ?? NewRandomString(),
                ConverterEnabled = _converterEnabled.GetValueOrDefault(NewRandomFlag())
            };
    }
}