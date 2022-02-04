using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.Versioning;
using CalculateFunding.Common.Utility;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class SpecificationRepositoryStepDefinitions
    {
        private readonly ICurrentSpecificationStepContext _currentSpecificationContext;

        public SpecificationRepositoryStepDefinitions(ICurrentSpecificationStepContext specificationStepContext)
        {
            _currentSpecificationContext = specificationStepContext;
        }

        [Given(@"the following specification exists")]
        public async Task GivenTheFollowingSpecificationExists(Table table)
        {
            SpecificationSummary specificationSummary = table.CreateInstance<SpecificationSummary>();

            string fundingStreamId = null;
            string fundingPeriodId = null;
            string templateVersion = null;

            foreach (TableRow row in table.Rows)
            {
                if (row.Count < 2)
                {
                    continue;
                }

                if (string.Equals(row[0], "FundingStreamId", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    fundingStreamId = row[1];
                }

                if (string.Equals(row[0], "FundingPeriodId", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    fundingPeriodId = row[1];
                }

                if (string.Equals(row[0], "TemplateVersion", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    templateVersion = row[1];
                }
            }

            if (!string.IsNullOrWhiteSpace(fundingStreamId))
            {
                specificationSummary.FundingStreams = new Reference[] { new Reference(fundingStreamId, fundingStreamId) };

                if (!string.IsNullOrWhiteSpace(templateVersion))
                {
                    specificationSummary.TemplateIds = new Dictionary<string, string>() { { fundingStreamId, templateVersion } };
                }
            }

            if (!string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                specificationSummary.FundingPeriod =  new Reference(fundingPeriodId, fundingPeriodId);
            }

            await _currentSpecificationContext.Repo.AddSpecification(specificationSummary);

            _currentSpecificationContext.SpecificationId = specificationSummary.Id;
        }

        [Given(@"the specification has the funding period with id '(.*)' and name '(.*)'")]
        public async Task GivenTheSpecificationHasTheFundingPeriodWithIdAndName(string fundingPeriodId, string fundingPeriodName)
        {
            Guard.IsNullOrWhiteSpace(_currentSpecificationContext.SpecificationId, nameof(_currentSpecificationContext.SpecificationId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingPeriodName, nameof(fundingPeriodName));

            SpecificationSummary specificationSummary = await _currentSpecificationContext.Repo.GetSpecificationSummaryById(_currentSpecificationContext.SpecificationId);
            specificationSummary
                .Should()
                .NotBeNull();

            specificationSummary.FundingPeriod = new Common.Models.Reference(fundingPeriodId, fundingPeriodName);
        }

        [Given(@"the specification has the following funding streams")]
        public async Task GivenTheSpecificationHasTheFollowingFundingStreams(Table table)
        {
            IEnumerable<Reference> fundingStreams = table.CreateSet<Reference>();

            Guard.IsNullOrWhiteSpace(_currentSpecificationContext.SpecificationId, nameof(_currentSpecificationContext.SpecificationId));
            SpecificationSummary specificationSummary = await _currentSpecificationContext.Repo.GetSpecificationSummaryById(_currentSpecificationContext.SpecificationId);
            specificationSummary
                .Should()
                .NotBeNull();

            specificationSummary.FundingStreams = fundingStreams;
        }

        [Given(@"the specification has the following template versions for funding streams")]
        public async Task GivenTheSpecificationHasTheFollowingTemplateVersionsForFundingStreams(Table table)
        {
            Guard.IsNullOrWhiteSpace(_currentSpecificationContext.SpecificationId, nameof(_currentSpecificationContext.SpecificationId));
            SpecificationSummary specificationSummary = await _currentSpecificationContext.Repo.GetSpecificationSummaryById(_currentSpecificationContext.SpecificationId);
            specificationSummary
                .Should()
                .NotBeNull();

            IEnumerable<KeyValuePair<string, string>> templateVersions = table.CreateSet<KeyValuePair<string, string>>();

            templateVersions
                .Should()
                .NotBeNull();

            specificationSummary.TemplateIds = new Dictionary<string, string>(templateVersions);
        }

        [Given(@"the specification is approved")]
        public void GivenTheSpecificationIsApproved()
        {
            Guard.IsNullOrWhiteSpace(_currentSpecificationContext.SpecificationId, nameof(_currentSpecificationContext.SpecificationId));

            _currentSpecificationContext.Repo.EditSpecificationStatus(
                _currentSpecificationContext.SpecificationId,
                PublishStatus.Approved);
        }
    }
}
