namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Cached Job entity - current version from Cosmos - supports Null jobs
    /// </summary>
    public class JobCacheItem
    {
        public Job Job { get; set; }
    }
}
