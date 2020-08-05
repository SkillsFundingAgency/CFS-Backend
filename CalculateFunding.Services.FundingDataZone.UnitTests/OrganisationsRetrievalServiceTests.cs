using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    [TestClass]
    public class OrganisationsRetrievalServiceTests
    {
        private Mock<IPublishingAreaRepository> _publishingArea;
        private Mock<IMapper> _mapper;

        private OrganisationsRetrievalService _service;

        [TestInitialize]
        public void SetUp()
        {
            _publishingArea = new Mock<IPublishingAreaRepository>();
            _mapper = new Mock<IMapper>();
            
            _service = new OrganisationsRetrievalService(_publishingArea.Object,
                _mapper.Object);
        }

        [TestMethod]
        public async Task GetAllOrganisations()
        {
            PublishingAreaOrganisation areaOrganisationOne = NewPublishingAreaOrganisation();
            PublishingAreaOrganisation areaOrganisationTwo = NewPublishingAreaOrganisation();
            PublishingAreaOrganisation areaOrganisationThree = NewPublishingAreaOrganisation();

            int snapshotId = NewRandomNumber();

            PaymentOrganisation organisationOne = NewPaymentOrganisation();
            PaymentOrganisation organisationTwo = NewPaymentOrganisation();
            PaymentOrganisation organisationThree = NewPaymentOrganisation();
            
            GivenThePublishingAreaOrganisationsForSnapshotId(snapshotId, areaOrganisationOne, areaOrganisationTwo, areaOrganisationThree);
            AndTheMappings((areaOrganisationOne, organisationOne),
                (areaOrganisationTwo, organisationTwo),
                (areaOrganisationThree, organisationThree));

            IEnumerable<PaymentOrganisation> actualOrganisations = await WhenTheOrganisationsAreQueried(snapshotId);

            actualOrganisations
                .Should()
                .BeEquivalentTo<PaymentOrganisation>(new[]
                {
                    organisationOne, organisationTwo, organisationThree
                });
        }

        private async Task<IEnumerable<PaymentOrganisation>> WhenTheOrganisationsAreQueried(int snapshotId)
            => await _service.GetAllOrganisations(snapshotId);

        private void GivenThePublishingAreaOrganisationsForSnapshotId(int snapshotId,
            params PublishingAreaOrganisation[] organisations)
        {
            _publishingArea.Setup(_ => _.GetAllOrganisations(snapshotId))
                .ReturnsAsync(organisations);
        }
        
        private int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

        private void AndTheMappings(params (PublishingAreaOrganisation source, PaymentOrganisation destination)[] mappings)
        {
            foreach ((PublishingAreaOrganisation source, PaymentOrganisation destination) mapping in mappings)
            {
                _mapper.Setup(_ => _.Map<PaymentOrganisation>(mapping.source))
                    .Returns(mapping.destination);
            }
        }

        private PublishingAreaOrganisation NewPublishingAreaOrganisation() => new PublishingAreaOrganisationBuilder()
            .Build();
        
        private PaymentOrganisation NewPaymentOrganisation() => new PaymentOrganisationBuilder()
            .Build();
    }
}