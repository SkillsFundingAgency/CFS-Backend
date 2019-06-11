using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class PublishedAllocationLineLogicalResultVersionService : IPublishedAllocationLineLogicalResultVersionService
    {
        public void SetVersion(PublishedAllocationLineResultVersion allocationLineResultVersion)
        {
            Guard.ArgumentNotNull(allocationLineResultVersion, nameof(allocationLineResultVersion));

            int major = allocationLineResultVersion.Major;
            int minor = allocationLineResultVersion.Minor;

            AllocationLineStatus allocationLineStatus = allocationLineResultVersion.Status;

            if (allocationLineStatus == AllocationLineStatus.Published && major == 0)
            {
                major = 1;
                minor = 0;
            }
            else if (allocationLineStatus == AllocationLineStatus.Published && major > 0)
            {
                major += 1;
                minor = 0;
            }
            else
            {
                minor += 1;
            }

            allocationLineResultVersion.Major = major;
            allocationLineResultVersion.Minor = minor;
        }
    }
}
