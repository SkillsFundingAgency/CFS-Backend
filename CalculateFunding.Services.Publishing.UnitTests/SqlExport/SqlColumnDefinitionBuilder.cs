using CalculateFunding.Models.Publishing.SqlExport;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    public class SqlColumnDefinitionBuilder : TestEntityBuilder
    {
        private string _name;
        private string _type;
        private bool? _allowNulls;

        public SqlColumnDefinitionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public SqlColumnDefinitionBuilder WithType(string type)
        {
            _type = type;

            return this;
        }

        public SqlColumnDefinitionBuilder WithAllowNulls(bool allowNulls)
        {
            _allowNulls = allowNulls;

            return this;
        }
        
        public SqlColumnDefinition Build()
        {
            return  new SqlColumnDefinition
            {
                Name = _name ?? NewRandomString(),
                Type = _type ?? NewRandomString(),
                AllowNulls = _allowNulls.GetValueOrDefault(NewRandomFlag())
            };
        }
    }
}