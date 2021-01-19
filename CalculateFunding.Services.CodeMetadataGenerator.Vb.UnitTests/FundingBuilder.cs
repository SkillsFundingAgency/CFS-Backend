using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    public class FundingBuilder
    {
        private IEnumerable<FundingLine> _fundingLines;
        private IDictionary<uint, string> _mappings;

        public FundingBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public FundingBuilder WithMappings(IDictionary<uint, string> mappings)
        {
            _mappings = mappings;

            return this;
        }

        public Funding Build()
        {
            return new Funding
            {
                FundingLines = _fundingLines ?? Enumerable.Empty<FundingLine>(),
                Mappings = _mappings ?? new Dictionary<uint, string>()
            };
        }
    }
}
