using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Api.Specs.Controllers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Api.Specs.UnitTests
{

    [TestClass]
    public class SpecificationProviderControllerTests
    {
        private Mock<IQueueCreateSpecificationJobActions> _specActions;
        private SpecificationProviderController _controller;
        private DefaultHttpContext _httpContext;
        private ControllerContext _controllerContext;
        private ProviderSnapshotDataLoadRequest _request;
        private RandomString _userId;
        private RandomString _userName;
        private string _correlationId;
        private string _specificationId;
        private string _fundingStreamId;
        private int _providerSnapshotId;

        [TestInitialize]
        public void Setup()
        {
            _specificationId = new RandomString();
            _fundingStreamId = new RandomString();
            _providerSnapshotId = new RandomNumberBetween(1, int.MaxValue);

            _request = new ProviderSnapshotDataLoadRequest()
            {
                SpecificationId = _specificationId,
                FundingStreamId = _fundingStreamId,
                ProviderSnapshotId = _providerSnapshotId,
            };

            _specActions = new Mock<IQueueCreateSpecificationJobActions>();
            _controller = new SpecificationProviderController(_specActions.Object);

            _httpContext = new DefaultHttpContext();
            _controllerContext = new ControllerContext()
            {
                HttpContext = _httpContext,
            };

            _controller.ControllerContext = _controllerContext;
            _correlationId = new RandomString();

            _userId = new RandomString();
            _userName = new RandomString();
        }

        [TestMethod]
        public async Task CreateProviderSnapshotDataLoadJob_DelegatesToService()
        {
            GivenAUserIsSpecifiedInRequest();
            AndACorrelationIdIsSpecifiedInRequest();

            await WhenCreateProviderSnapshotDataLoadJob();

            ThenTheServiceIsCalled();
        }

        private void AndACorrelationIdIsSpecifiedInRequest()
        {
            _httpContext.Request.Headers.Add("sfa-correlationId", _correlationId);
        }

        private void ThenTheServiceIsCalled()
        {
            _specActions.Verify(_ =>
            _.CreateProviderSnapshotDataLoadJob(_request,
            It.Is<CalculateFunding.Common.Models.Reference>(r => r.Id == _userId && r.Name == _userName),
            _correlationId), Times.Once);
        }

        private void GivenAUserIsSpecifiedInRequest()
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Sid, _userId),
                new Claim(ClaimTypes.Name, _userName)
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuthType");
            _httpContext.User = new ClaimsPrincipal(identity);
        }

        private async Task WhenCreateProviderSnapshotDataLoadJob()
        {
            await _controller.CreateProviderSnapshotDataLoadJob(_request);
        }
    }
}
