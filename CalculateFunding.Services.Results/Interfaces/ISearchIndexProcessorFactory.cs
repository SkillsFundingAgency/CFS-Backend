namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexProcessorFactory
    {
        ISearchIndexProcessor CreateProcessor(string indexWriterType);
    }
}
