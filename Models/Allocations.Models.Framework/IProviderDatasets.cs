namespace Allocations.Models.Framework
{
    public interface IProviderDataset
    {
        string ModelName { get; }
        string DatasetName { get; }
        string URN { get; }
    }

    public interface IAuthorityDataset
    {
        string ModelName { get; }
        string URN { get; }
    }
}