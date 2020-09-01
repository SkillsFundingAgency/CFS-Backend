using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProfilingAuditBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private Reference _user;
        private DateTime _date;

        public ProfilingAuditBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ProfilingAuditBuilder WithUser(Reference user)
        {
            _user = user;

            return this;
        }

        public ProfilingAuditBuilder WithDate(DateTime date)
        {
            _date = date;

            return this;
        }

        public ProfilingAudit Build()
        {
            return new ProfilingAudit
            {
                FundingLineCode = _fundingLineCode,
                User = _user,
                Date = _date
            };
        }

    }
}
