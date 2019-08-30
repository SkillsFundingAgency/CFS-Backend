namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFeedItemPreloaderSettings
    {
        bool ShouldPreLoad { get; }
        int PreLoadCount { get; }
        int PageSize { get; }
    }
}