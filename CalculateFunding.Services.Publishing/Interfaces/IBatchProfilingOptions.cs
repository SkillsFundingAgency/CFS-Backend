namespace CalculateFunding.Services.Publishing
{
    public interface IBatchProfilingOptions
    {
        int BatchSize { get; }
        
        int ConsumerCount { get; }
    }
}