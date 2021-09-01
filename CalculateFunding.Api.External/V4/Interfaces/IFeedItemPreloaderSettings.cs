namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IFeedItemPreloaderSettings
    {
        bool ShouldPreLoad { get; }
        int PreLoadCount { get; }
        int PageSize { get; }
    }
}