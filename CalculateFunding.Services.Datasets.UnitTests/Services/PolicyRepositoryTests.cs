using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class PolicyRepositoryTests
    {
        private IPoliciesApiClient _policiesApiClient;
        private PolicyRepository _policyRepository;

        private string _fundingStreamId;
        private string _fundingStreamName;

        [TestInitialize]
        public void Initialize()
        {
            _fundingStreamId = NewRandomString();
            _fundingStreamName = NewRandomString();

            _policiesApiClient = Substitute.For<IPoliciesApiClient>();

            _policyRepository = new PolicyRepository(
                _policiesApiClient,
                DatasetsResilienceTestHelper.GenerateTestPolicies());
        }

        [TestMethod]
        public async Task GetFundingStreams_Should_Return_FundingStreams()
        {
            IEnumerable<PoliciesApiModels.FundingStream> apiFundingStreams =
                new List<PoliciesApiModels.FundingStream>
            {
                NewApiFundingStream(_ => _.WithId(_fundingStreamId).WithName(_fundingStreamName))
            };

            GivenGetFundingStreams(apiFundingStreams);

            IEnumerable<PoliciesApiModels.FundingStream> fundingStreams
                = await WhenGetFundingStreams();

            ThenFundingStreamResultMatches(fundingStreams);
        }

        private void ThenFundingStreamResultMatches(IEnumerable<PoliciesApiModels.FundingStream> fundingStreams)
        {
            fundingStreams
                .Count()
                .Should()
                .Be(1);

            fundingStreams
                .FirstOrDefault()
                .Should()
                .NotBeNull();

            PoliciesApiModels.FundingStream fundingStream = fundingStreams.FirstOrDefault();

            fundingStream
                .Id
                .Should()
                .Be(_fundingStreamId);

            fundingStream
                .Name
                .Should()
                .Be(_fundingStreamName);
        }

        private async Task<IEnumerable<PoliciesApiModels.FundingStream>> WhenGetFundingStreams()
        {
            return await _policyRepository.GetFundingStreams();
        }

        private void GivenGetFundingStreams(IEnumerable<PoliciesApiModels.FundingStream> fundingStreams )
        {
            _policiesApiClient
                .GetFundingStreams()
                .Returns(new ApiResponse<IEnumerable<PoliciesApiModels.FundingStream>>(System.Net.HttpStatusCode.OK, fundingStreams));
        }

        private PoliciesApiModels.FundingStream NewApiFundingStream(
            Action<PolicyFundingStreamBuilder> setUp = null)
        {
            PolicyFundingStreamBuilder fundingStreamBuilder = new PolicyFundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
