using AutoMapper;
using CalculateFunding.Api.External.V3.MappingProfiles;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    [TestClass]
    public class FundingStreamServiceTests
    {
        private IPoliciesApiClient _policiesApiClient;
        private IMapper _mapper;
        private FundingStreamService _fundingStreamService;

        private string _id;
        private string _name;

        [TestInitialize]
        public void Initialize()
        {
            _id = NewRandomString();
            _name = NewRandomString();

            _policiesApiClient = Substitute.For<IPoliciesApiClient>();
            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<ExternalServiceMappingProfile>();
            }).CreateMapper();

            _fundingStreamService = new FundingStreamService(
                _policiesApiClient,
                new ExternalApiResiliencePolicies
                {
                    PoliciesApiClientPolicy = Polly.Policy.NoOpAsync()
                },
                _mapper
                );
        }

        [TestMethod]
        public async Task ReturnsFundingStreams()
        {
            IEnumerable<Common.ApiClient.Policies.Models.FundingStream> apiFundingStreams = 
                new List<Common.ApiClient.Policies.Models.FundingStream>
            {
                NewApiFundingStream(_ => _.WithId(_id).WithName(_name))
            };

            GivenApiFundingStreams(apiFundingStreams);

            IActionResult result = await WhenGetFundingStreams();

            ThenFundingStreamResultMatches(result);
        }

        private void GivenApiFundingStreams(IEnumerable<Common.ApiClient.Policies.Models.FundingStream> fundingStreams)
        {
            _policiesApiClient
                .GetFundingStreams()
                .Returns(
                Task.FromResult(
                    new ApiResponse<IEnumerable<Common.ApiClient.Policies.Models.FundingStream>>(
                        HttpStatusCode.OK,
                        fundingStreams)));
        }

        private async Task<IActionResult> WhenGetFundingStreams()
        {
            return await _fundingStreamService.GetFundingStreams();
        }

        private void ThenFundingStreamResultMatches(IActionResult result)
        {
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<List<FundingStream>>();

            IEnumerable<FundingStream> fundingStreams = (result as OkObjectResult).Value as IEnumerable<FundingStream>;

            fundingStreams
                .Count()
                .Should()
                .Be(1);

            fundingStreams
                .FirstOrDefault()
                .Should()
                .NotBeNull();

            FundingStream fundingStream = fundingStreams.FirstOrDefault();

            fundingStream
                .Id
                .Should()
                .Be(_id);

            fundingStream
                .Name
                .Should()
                .Be(_name);
        }

        private FundingStream NewFundingStream(Action<FundingStreamBuilder> setUp = null)
        {
            FundingStreamBuilder fundingStreamBuilder = new FundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        private Common.ApiClient.Policies.Models.FundingStream NewApiFundingStream(Action<PolicyFundingStreamBuilder> setUp = null)
        {
            PolicyFundingStreamBuilder fundingStreamBuilder = new PolicyFundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
