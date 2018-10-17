using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IPublishedAllocationLineLogicalResultVersionService
    {
        void SetVersion(PublishedAllocationLineResultVersion allocationLineResultVersion);
    }
}
