using CalculateFunding.Models.Users;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Users
{
    public class UserPermissionCsvGenerationMessageBuilder : TestEntityBuilder
    {
        private string _environment;
        private string _fundingStreamId;
        private DateTimeOffset _reportRunTime;

        public UserPermissionCsvGenerationMessageBuilder WithEnvironment(string environment)
        {
            _environment = environment;

            return this;
        }

        public UserPermissionCsvGenerationMessageBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public UserPermissionCsvGenerationMessageBuilder WithReportRunTime(DateTimeOffset reportRunTime)
        {
            _reportRunTime = reportRunTime;

            return this;
        }

        public UserPermissionCsvGenerationMessage Build()
        {
            return new UserPermissionCsvGenerationMessage
            {
                Environment = _environment,
                FundingStreamId = _fundingStreamId,
                ReportRunTime = _reportRunTime
            };
        }
    }
}
