using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class DatasetDefinitionTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _description;
        private string _fundingStreamId;
        private bool _converterEnabled;
        private int _version;
        private TableDefinition[] _tableDefinitions;
        private string _name;

        public DatasetDefinitionTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithTableDefinitions(params TableDefinition[] tableDefinitions)
        {
            _tableDefinitions = tableDefinitions;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithConverterEnabled(bool converterEnabled)
        {
            _converterEnabled = converterEnabled;

            return this;
        }

        public DatasetDefinitionTemplateParametersBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetDefinitionTemplateParameters Build() =>
            new DatasetDefinitionTemplateParameters
            {
                Id = _id ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Version = _version,
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                ConverterEnabled = _converterEnabled,
                TableDefinitions = _tableDefinitions ?? new TableDefinition[0]
            };
    }
}