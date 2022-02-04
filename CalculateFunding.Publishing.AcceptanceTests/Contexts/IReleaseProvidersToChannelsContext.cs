using CalculateFunding.Models.Publishing.FundingManagement;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IReleaseProvidersToChannelsContext
    {
        ReleaseProvidersToChannelRequest Request { get; set; }
    }
}