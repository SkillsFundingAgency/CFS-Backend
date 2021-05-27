using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ConverterActivityReportRowParametersBuilder : TestEntityBuilder
    {
        private string _ukprn;
        private string _name;
        private string _status;
        private DateTimeOffset? _targetOpeningDate;
        private string _ineligible;
        private string _sourceProviderUKPRN;

        public ConverterActivityReportRowParametersBuilder WithUkprn(string ukprn)
        {
            _ukprn = ukprn;

            return this;
        }

        public ConverterActivityReportRowParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ConverterActivityReportRowParametersBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }

        public ConverterActivityReportRowParametersBuilder WithTargetDate(DateTimeOffset targetOpeningDate)
        {
            _targetOpeningDate = targetOpeningDate;

            return this;
        }

        public ConverterActivityReportRowParametersBuilder WithInEligible(string ineligible)
        {
            _ineligible = ineligible;

            return this;
        }

        public ConverterActivityReportRowParametersBuilder WithSourceProviderUKPRN(string sourceProviderUKPRN)
        {
            _sourceProviderUKPRN = sourceProviderUKPRN;

            return this;
        }

        public ConverterActivityReportRowParameters Build() =>
            new ConverterActivityReportRowParameters
            {
                TargetUKPRN = _ukprn ?? NewRandomString(),
                TargetProviderName = _name ?? NewRandomString(),
                TargetOpeningDate = (_targetOpeningDate ?? NewRandomDateTime()).ToString(),
                TargetProviderIneligible = _ineligible,
                TargetProviderStatus = _status,
                SourceProviderUKPRN = _sourceProviderUKPRN ?? NewRandomString()
            };
    }
}