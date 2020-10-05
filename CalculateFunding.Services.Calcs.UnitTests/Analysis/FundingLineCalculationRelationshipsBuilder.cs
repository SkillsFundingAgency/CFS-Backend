using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class FundingLineCalculationRelationshipsBuilder : TestEntityBuilder
    {
        private string _calculationOneId;
        private string _calculationTwoId;
        private FundingLine _fundingLine;

        public FundingLineCalculationRelationshipsBuilder WithCalculationOneId(string id)
        {
            _calculationOneId = id;

            return this;
        }

        public FundingLineCalculationRelationshipsBuilder WithCalculationTwoId(string id)
        {
            _calculationTwoId = id;

            return this;
        }

        public FundingLineCalculationRelationshipsBuilder WithFundingLine(FundingLine fundingLine)
        {
            _fundingLine = fundingLine;

            return this;
        }

        public FundingLineCalculationRelationship Build()
        {
            return new FundingLineCalculationRelationship
            {
                CalculationOneId = _calculationOneId ?? NewRandomString(),
                FundingLine = _fundingLine ?? new FundingLine(),
                CalculationTwoId = _calculationTwoId ?? NewRandomString(),
            };
        }
    }
}
