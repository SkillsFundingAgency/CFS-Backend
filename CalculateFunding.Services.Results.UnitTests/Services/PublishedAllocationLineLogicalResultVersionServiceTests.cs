using CalculateFunding.Models.Results;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    [TestClass]
    public class PublishedAllocationLineLogicalResultVersionServiceTests
    {
        [TestMethod]
        public void SetVersion_GivenResultHasBeenSetToPublish_IncreasesMajorVesionandSetsMinorToZero()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Published,
                Major = 0,
                Minor = 5
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(1);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(0);
        }

        [TestMethod]
        public void SetVersion_GivenResultIsInHeldStatus_IncreasesMinorVesion()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Held,
                Major = 0,
                Minor = 5
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(0);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(6);
        }

        [TestMethod]
        public void SetVersion_GivenResultIsInApprovedStatus_IncreasesMinorVesion()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Approved,
                Major = 0,
                Minor = 5
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(0);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(6);
        }

        [TestMethod]
        public void SetVersion_GivenResultIsInUpdatedStatusAndPublished_IncreasesMajorResetsMinor()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Published,
                Major = 1,
                Minor = 5
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(2);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(0);
        }

        [TestMethod]
        public void SetVersion_GivenResultIsInUpdatedStatusAndApproved_IncreasesMinorVesion()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Approved,
                Major = 0,
                Minor = 5
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(0);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(6);
        }

        [TestMethod]
        public void SetVersion_GivenNewlyHeldResult_IncreasesMinorVesionToOne()
        {
            //Arrange
            PublishedAllocationLineResultVersion allocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Status = AllocationLineStatus.Held,
                Major = 0,
                Minor = 0
            };

            PublishedAllocationLineLogicalResultVersionService service = new PublishedAllocationLineLogicalResultVersionService();

            //Act
            service.SetVersion(allocationLineResultVersion);

            //Assert
            allocationLineResultVersion
                .Major
                .Should()
                .Be(0);

            allocationLineResultVersion
                .Minor
                .Should()
                .Be(1);
        }
    }
}
